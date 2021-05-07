using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PostgreApi.Data;
using PostgreApi.Models;

namespace PostgreApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TeachersController : ControllerBase
    {
        private readonly DataContext _context;
        public static IWebHostEnvironment _environment;
        public TeachersController(DataContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: api/Teachers
        [HttpGet]
        [Authorize(Roles = Util.Role.Admin + ", " + Util.Role.User + ", " + Util.Role.Root)]
        public async Task<ActionResult<IEnumerable<Teacher>>> GetTeachers()
        {
            return await _context.Teachers.Include(t => t.Courses).ToListAsync();
        }

        // GET: api/Teachers/5
        [HttpGet("{id}")]
        [Authorize(Roles = Util.Role.Admin + ", " + Util.Role.User + ", " + Util.Role.Root)]
        public async Task<ActionResult<Teacher>> GetTeacher(int id)
        {
            var teacher = await _context.Teachers.Include(t => t.Courses)
                                                 .SingleOrDefaultAsync(t => t.Id == id);

            if (teacher == null)
            {
                return NotFound();
            }

            return teacher;
        }

        // PUT: api/Teachers/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for
        // more details, see https://go.microsoft.com/fwlink/?linkid=2123754.
        [HttpPut("{id}")]
        [Authorize(Roles = Util.Role.Admin + ", " + Util.Role.User + ", " + Util.Role.Root)]
        public async Task<IActionResult> PutTeacher(int id, [FromForm] [Bind("Id,Name, LastName, MaidenName, Gender, Files")]Teacher teacher)
        {
            if (id != teacher.Id)
            {
                return BadRequest();
            }
            var teacherS = await _context.Teachers.SingleOrDefaultAsync(x => x.Id == id);
            string FileName = "";
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }
            if (teacher.Files != null)
            {
                if (teacher.Files.Length > 0)
                {
                    try
                    {
                        if (!Directory.Exists(_environment.WebRootPath + ("\\img\\Teachers\\" + teacherS.IdPath + "\\").Replace(" ", "")))
                        {
                            Directory.CreateDirectory(_environment.WebRootPath + ("\\img\\Teachers\\" + teacherS.IdPath + "\\").Replace(" ", ""));
                        }
                        using (FileStream fileStream = System.IO.File.Create(_environment.WebRootPath + ("\\img\\Teachers\\" + teacherS.IdPath + "\\").Replace(" ", "") + "profile.jpg"))
                        {
                            FileName = $"{this.Request.Scheme}://{this.Request.Host}{this.Request.PathBase}/img/Teachers/{teacherS.IdPath}/profile.jpg";
                            await teacher.Files.CopyToAsync(fileStream);
                            await fileStream.FlushAsync();
                            teacher.ImgURL = FileName;
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
                
                teacher.ImgURL = teacherS.ImgURL;
                
            }
            teacher.IdPath = teacherS.IdPath;
            _context.Entry(teacherS).CurrentValues.SetValues(teacher);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TeacherExists(id))
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

        // POST: api/Teachers
        // To protect from overposting attacks, enable the specific properties you want to bind to, for
        // more details, see https://go.microsoft.com/fwlink/?linkid=2123754.
        [HttpPost]
        [Authorize(Roles = Util.Role.Admin + ", " + Util.Role.User + ", " + Util.Role.Root)]
        public async Task<ActionResult<Teacher>> PostTeacher([FromForm] [Bind("Name, LastName, MaidenName, Gender, Files")]Teacher teacher)
        {
            string FileName = "";
            string Id = Guid.NewGuid().ToString();
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }
            if (teacher.Files != null)
            {
                if (teacher.Files.Length > 0)
                {
                    try
                    {
                        if (!Directory.Exists(_environment.WebRootPath + ("\\img\\Teachers\\" + Id + "\\").Replace(" ", "")))
                        {
                            Directory.CreateDirectory(_environment.WebRootPath + ("\\img\\Teachers\\" + Id + "\\").Replace(" ", ""));
                        }
                        using (FileStream fileStream = System.IO.File.Create(_environment.WebRootPath + ("\\img\\Teachers\\" + Id + "\\").Replace(" ", "") + "profile.jpg"))
                        {
                            FileName = $"{this.Request.Scheme}://{this.Request.Host}{this.Request.PathBase}/img/Teachers/{Id}/profile.jpg";
                            await teacher.Files.CopyToAsync(fileStream);
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
                FileName = $"{this.Request.Scheme}://{this.Request.Host}{this.Request.PathBase}/img/Teachers/default.png";

            teacher.IdPath = Id;
            teacher.ImgURL = FileName;
            teacher.Files = null;

            _context.Teachers.Add(teacher);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetTeacher", new { id = teacher.Id }, teacher);
        }

        // DELETE: api/Teachers/5
        [HttpDelete("{id}")]
        [Authorize(Roles = Util.Role.Admin + ", " + Util.Role.User + ", " + Util.Role.Root)]
        public async Task<ActionResult<Teacher>> DeleteTeacher(int id)
        {
            var teacher = await _context.Teachers.FindAsync(id);
            if (teacher == null)
            {
                return NotFound();
            }
            if (Directory.Exists(_environment.WebRootPath + $"\\img\\Teachers\\{teacher.IdPath}\\"))
            {
                System.IO.File.Delete(_environment.WebRootPath + $"\\img\\Teachers\\{teacher.IdPath}\\profile.jpg");
                Directory.Delete(_environment.WebRootPath + $"\\img\\Teachers\\{teacher.IdPath}\\");
            }

            _context.Teachers.Remove(teacher);
            await _context.SaveChangesAsync();

            return teacher;
        }

        private bool TeacherExists(int id)
        {
            return _context.Teachers.Any(e => e.Id == id);
        }
    }
}
