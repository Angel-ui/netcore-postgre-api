using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PostgreApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Security.Cryptography;
using PostgreApi.Data;
using Microsoft.AspNetCore.Hosting;
using System.IO;

namespace PostgreApi.Controllers
{
   // [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly DataContext _db;
        private readonly JWTSettings _jwtsettings;
        public static IWebHostEnvironment _environment;

        public UsersController(IOptions<JWTSettings> jwtsettings, DataContext db, IWebHostEnvironment environment)
        {
            _db = db;
            _jwtsettings = jwtsettings.Value;
            _environment = environment;
        }

        // GET: api/Users
  
        [HttpGet]
        [Authorize(Roles = Util.Role.Admin +", " + Util.Role.Root)]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            var claims = (ClaimsIdentity)User.Identity;
            var all = claims.Claims.ElementAt(1).Value == "Root" ?
                    await _db.Users.Include(u => u.Role).Where(u => u.Role.RoleDesc != "Root").ToListAsync() :
                    await _db.Users.Include(u => u.Role).Where(u => u.Role.RoleDesc == "User").ToListAsync();
            return Ok(all);

        }

        // GET: api/Users/5
        [HttpGet("{id}")]
        [Authorize(Roles = Util.Role.Admin + ", " + Util.Role.Root)]
        public async Task<ActionResult<User>> GetUser(int id)
        {

            var claims = (ClaimsIdentity)User.Identity;
            var user = claims.Claims.ElementAt(1).Value == "Root" ?
                                await _db.Users.Include(u => u.Role)
                                        .Where(u => u.Id == id && u.Role.RoleDesc != "Root")
                                        .FirstOrDefaultAsync() :
                                await _db.Users.Include(u => u.Role)
                                        .Where(u => u.Id == id && u.Role.RoleDesc == "User")
                                        .FirstOrDefaultAsync();
            if (user == null)
            {
                return NotFound();
            }

            return user;
            
        }

        // POST: api/Users
        [HttpPost("Login")]
        public async Task<ActionResult<UserWithToken>> Login([FromBody][Bind("EmailAddress", "Password")] User user)
        {
            user = await _db.Users
                            .Include(u => u.Role)
                            .Where(u => u.EmailAddress == user.EmailAddress
                                && u.Password == Crypto.Hash(user.Password))
                            .FirstOrDefaultAsync();
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            UserWithToken userWithToken = null;

            if (user != null)
            {
                RefreshToken refreshToken = GenerateRefreshToken();
                //user.RefreshTokens.Add(refreshToken);
                //await _db.SaveChangesAsync();

                userWithToken = new UserWithToken(user)
                {
                    RefreshToken = refreshToken.Token
                };
            }
                
            if (userWithToken == null)
            {
                return NotFound();
            }

            //sign your token here...
            userWithToken.AccessToken = GenerateAccessToken(user.Id);
            return userWithToken;
            
        }

        // GET: api/Users
        [HttpPost("RefreshToken")]
        public async Task<ActionResult<UserWithToken>> RefreshToken([FromBody] RefreshRequest refreshRequest)
        {
            User user = await GetUserFromAccessToken(refreshRequest.AccessToken);

            if (user != null && ValidateRefreshToken(user, refreshRequest.RefreshToken))
            {
                UserWithToken userWithToken = new UserWithToken(user)
                {
                    AccessToken = GenerateAccessToken(user.Id)
                };

                return userWithToken;
            }

            return null;
        }
        private bool ValidateRefreshToken(User user, string refreshToken)
        {
            
                RefreshToken refreshTokenUser = _db.RefreshTokens.Where(rt => rt.Token == refreshToken)
                                                .OrderByDescending(rt => rt.ExpiryDate)
                                                .FirstOrDefault();

                if (refreshTokenUser != null && refreshTokenUser.UserId == user.Id
                    && refreshTokenUser.ExpiryDate > DateTime.UtcNow)
                {
                    return true;
                }

                return false;
            
        }

        private async Task<User> GetUserFromAccessToken(string accessToken)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_jwtsettings.SecretKey);

                var tokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false
                };

                SecurityToken securityToken;
                var principle = tokenHandler.ValidateToken(accessToken, tokenValidationParameters, out securityToken);

                JwtSecurityToken jwtSecurityToken = securityToken as JwtSecurityToken;

                if (jwtSecurityToken != null && jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    
                        var userId = principle.FindFirst(ClaimTypes.Name)?.Value;

                        return await _db.Users.Include(u => u.Role)
                                            .Where(u => u.Id == Convert.ToInt32(userId)).FirstOrDefaultAsync();
                    
                }
            }
            catch (Exception)
            {
                return new User();
            }

            return new User();
        }
        private RefreshToken GenerateRefreshToken()
        {
            RefreshToken refreshToken = new RefreshToken();

            var randomNumber = new Byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                refreshToken.Token = Convert.ToBase64String(randomNumber);
            }
            refreshToken.ExpiryDate = DateTime.UtcNow.AddHours(5);

            return refreshToken;
        }

        private string GenerateAccessToken(int userId)
        {
            var user = _db.Users.SingleOrDefault(x => x.Id == userId );
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtsettings.SecretKey);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                    {
                            new Claim(ClaimTypes.Name, Convert.ToString(userId)),
                            new Claim(ClaimTypes.Role, Convert.ToString(user.Role.RoleDesc))
                    }),
                Expires = DateTime.UtcNow.AddMonths(6),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }


        [HttpPut("{id}")]
        [Authorize(Roles = Util.Role.Admin + ", " + Util.Role.Root)]
        public async Task<IActionResult> PutUser([FromRoute]int id, [FromForm] [Bind("Id,EmailAddress, Password, FirstName, MiddleName, LastName, RoleId, Files")]User user)
        {
            if (id != user.Id)
            {
                return BadRequest();
            }
            var claims = (ClaimsIdentity)User.Identity;
            var userS = claims.Claims.ElementAt(1).Value == "Root" ?
                        await _db.Users.Where(u => u.Id == id && u.Role.RoleDesc != "Root")
                                                .FirstOrDefaultAsync() :
                        await _db.Users.Where(u => u.Id == id && u.Role.RoleDesc == "User")
                                                .FirstOrDefaultAsync();


            if (userS == null)
            {
                return NotFound();
            }

            user.RoleId = user.RoleId == 3 ? userS.RoleId : user.RoleId;
            string FileName = "";
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }
            if (user.Files != null)
            {
                if (user.Files.Length > 0)
                {
                    try
                    {
                        if (!Directory.Exists(_environment.WebRootPath + ("\\img\\Users\\" + userS.IdPath + "\\").Replace(" ", "")))
                        {
                            Directory.CreateDirectory(_environment.WebRootPath + ("\\img\\Users\\" + userS.IdPath + "\\").Replace(" ", ""));
                        }
                        using (FileStream fileStream = System.IO.File.Create(_environment.WebRootPath + ("\\img\\Users\\" + userS.IdPath + "\\").Replace(" ", "") + "profile.jpg"))
                        {
                            FileName = $"{this.Request.Scheme}://{this.Request.Host}{this.Request.PathBase}/img/Users/{userS.IdPath}/profile.jpg";
                            await user.Files.CopyToAsync(fileStream);
                            await fileStream.FlushAsync();
                            user.ImgURL = FileName;
                            //return "\\Upload\\" + objFile.files.FileName;
                        }
                    }


                    catch (Exception e)
                    {
                        throw e;

                    }
                }

            }
            else
            {

                user.ImgURL = userS.ImgURL;
                
            }
            user.IdPath = userS.IdPath;
            user.Password = user.Password == user.Password ? user.Password : Crypto.Hash(user.Password);
            user.HireDate = userS.HireDate;
            _db.Entry(userS).CurrentValues.SetValues(user);


            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
            
        }

        // POST: api/Users
        // To protect from overposting attacks, enable the specific properties you want to bind to, for
        // more details, see https://go.microsoft.com/fwlink/?linkid=2123754.
        [HttpPost]
        [Authorize(Roles = Util.Role.Admin + ", " + Util.Role.Root)]
        public async Task<ActionResult<User>> PostUser([FromForm] [Bind("EmailAddress, Password, FirstName, MiddleName, LastName, RoleId, Files")]User user)
        {
            string FileName = "";
            string Id = Guid.NewGuid().ToString();
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }
            var claims = (ClaimsIdentity)User.Identity;
            user.RoleId = claims.Claims.ElementAt(1).Value == "Root" ?
                                user.RoleId == 3 ? 2 : user.RoleId : 2;
            if (user.Files != null)
            {
                if (user.Files.Length > 0)
                {
                    try
                    {
                        if (!Directory.Exists(_environment.WebRootPath + ("\\img\\Users\\" + Id + "\\").Replace(" ", "")))
                        {
                            Directory.CreateDirectory(_environment.WebRootPath + ("\\img\\Users\\" + Id + "\\").Replace(" ", ""));
                        }
                        using (FileStream fileStream = System.IO.File.Create(_environment.WebRootPath + ("\\img\\Users\\" + Id + "\\").Replace(" ", "") + "profile.jpg"))
                        {
                            FileName = $"{this.Request.Scheme}://{this.Request.Host}{this.Request.PathBase}/img/Users/{Id}/profile.jpg";
                            await user.Files.CopyToAsync(fileStream);
                            await fileStream.FlushAsync();
                            //return "\\Upload\\" + objFile.files.FileName;
                        }
                    }
                    catch (Exception e)
                    {
                        throw e;

                    }
                }
            }
            else
                FileName = $"{this.Request.Scheme}://{this.Request.Host}{this.Request.PathBase}/img/Users/default.png";

            user.IdPath = Id;
            user.ImgURL = FileName;
            user.Files = null;
            user.Password = Crypto.Hash(user.Password);
            user.HireDate = DateTime.Now;


            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            return CreatedAtAction("GetUser", new { id = user.Id }, user);
            
        }

        // DELETE: api/Users/5
        [HttpDelete("{id}")]
        [Authorize(Roles = Util.Role.Admin + ", " + Util.Role.Root)]
        public async Task<ActionResult<User>> DeleteUser(int id)
        {
            var claims = (ClaimsIdentity)User.Identity;
            var user = claims.Claims.ElementAt(1).Value == "Root" ?
                        await _db.Users.Where(u => u.Id == id && u.Role.RoleDesc != "Root")
                                                .FirstOrDefaultAsync() :
                        await _db.Users.Where(u => u.Id == id && u.Role.RoleDesc == "User")
                                .FirstOrDefaultAsync();
            if (user == null)
            {
                return NotFound();
            }
            if (Directory.Exists(_environment.WebRootPath + $"\\img\\Users\\{user.IdPath}\\"))
            {
                System.IO.File.Delete(_environment.WebRootPath + $"\\img\\Users\\{user.IdPath}\\profile.jpg");
                Directory.Delete(_environment.WebRootPath + $"\\img\\Users\\{user.IdPath}\\");
            }

            _db.Users.Remove(user);
            await _db.SaveChangesAsync();

            return user;
            
        }

        private bool UserExists(int id)
        {
           
                return _db.Users.Any(e => e.Id == id);
         
        }
    }
}
