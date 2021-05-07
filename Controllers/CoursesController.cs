using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    public class CoursesController : ControllerBase
    {
        private readonly DataContext _context;
        public static IWebHostEnvironment _environment;
        public CoursesController(DataContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }


        // GET: api/Courses
        [HttpGet]
        [Authorize(Roles = Util.Role.Admin + ", " + Util.Role.User + ", " + Util.Role.Root)]
        public async Task<ActionResult<IEnumerable<Course>>> GetCourses()
        {
            return await _context.Courses.Include(c => c.Teacher).Include(c => c.Category).ToListAsync();
        }

        // GET: api/Courses/5
        [HttpGet("{id}")]
        [Authorize(Roles = Util.Role.Admin + ", " + Util.Role.User + ", " + Util.Role.Root)]
        public async Task<ActionResult<Course>> GetCourse(int id)
        {
            var course = await _context.Courses.Include(c => c.Teacher).
                                                Include(c => c.Category).
                                                SingleOrDefaultAsync(c => c.Id == id);

            if (course == null)
            {
                return NotFound();
            }

            return course;
        }

        // PUT: api/Courses/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for
        // more details, see https://go.microsoft.com/fwlink/?linkid=2123754.
        [HttpPut("{id}")]
        [Authorize(Roles = Util.Role.Admin + ", " + Util.Role.User + ", " + Util.Role.Root)]
        public async Task<IActionResult> PutCourse(int id, [FromForm] [Bind("Id,Name, ClassesQty, Price, TeacherId, CategoryId,Files")]Course course)
        {
            if (id != course.Id)
            {
                return BadRequest();
            }

            var courseS = await _context.Courses.SingleOrDefaultAsync(c => c.Id == id);
            string FileName = "";
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }
            if (course.Files != null)
            {
                if (course.Files.Length > 0)
                {
                    try
                    {
                        if (!Directory.Exists(_environment.WebRootPath + ("\\img\\Courses\\" + courseS.IdPath + "\\").Replace(" ", "")))
                        {
                            Directory.CreateDirectory(_environment.WebRootPath + ("\\img\\Courses\\" + courseS.IdPath + "\\").Replace(" ", ""));
                        }
                        using (FileStream fileStream = System.IO.File.Create(_environment.WebRootPath + ("\\img\\Courses\\" + courseS.IdPath + "\\").Replace(" ", "") + "profile.jpg"))
                        {
                            FileName = $"{this.Request.Scheme}://{this.Request.Host}{this.Request.PathBase}/img/Courses/{courseS.IdPath}/profile.jpg";
                            await course.Files.CopyToAsync(fileStream);
                            await fileStream.FlushAsync();
                            course.ImgURL = FileName;
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

                course.ImgURL = courseS.ImgURL;

            }
            course.IdPath = courseS.IdPath;
            _context.Entry(courseS).CurrentValues.SetValues(course);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CourseExists(id))
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

        // POST: api/Courses
        // To protect from overposting attacks, enable the specific properties you want to bind to, for
        // more details, see https://go.microsoft.com/fwlink/?linkid=2123754.
        [HttpPost]
        [Authorize(Roles = Util.Role.Admin + ", " + Util.Role.User + ", " + Util.Role.Root)]
        public async Task<ActionResult<Course>> PostCourse([FromForm] [Bind("Name, ClassesQty, Price, TeacherId, CategoryId, Files")] Course course)
        {
            string FileName = "";
            string Id = Guid.NewGuid().ToString();
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }
            if (course.Files != null)
            {
                if (course.Files.Length > 0)
                {
                    try
                    {
                        if (!Directory.Exists(_environment.WebRootPath + ("\\img\\Courses\\" + Id + "\\").Replace(" ", "")))
                        {
                            Directory.CreateDirectory(_environment.WebRootPath + ("\\img\\Courses\\" + Id + "\\").Replace(" ", ""));
                        }
                        using (FileStream fileStream = System.IO.File.Create(_environment.WebRootPath + ("\\img\\Courses\\" + Id + "\\").Replace(" ", "") + "profile.jpg"))
                        {
                            FileName = $"{this.Request.Scheme}://{this.Request.Host}{this.Request.PathBase}/img/Courses/{Id}/profile.jpg";
                            await course.Files.CopyToAsync(fileStream);
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
                FileName = $"{this.Request.Scheme}://{this.Request.Host}{this.Request.PathBase}/img/Courses/default.png";

            course.IdPath = Id;
            course.ImgURL = FileName;
            course.Files = null;
            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetCourse", new { id = course.Id }, course);
        }

        // DELETE: api/Courses/5
        [HttpDelete("{id}")]
        [Authorize(Roles = Util.Role.Admin + ", " + Util.Role.User + ", " + Util.Role.Root)]
        public async Task<ActionResult<Course>> DeleteCourse(int id)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course == null)
            {
                return NotFound();
            }
            if (Directory.Exists(_environment.WebRootPath + $"\\img\\Courses\\{course.IdPath}\\"))
            {
                System.IO.File.Delete(_environment.WebRootPath + $"\\img\\Courses\\{course.IdPath}\\profile.jpg");
                Directory.Delete(_environment.WebRootPath + $"\\img\\Courses\\{course.IdPath}\\");
            }
            _context.Courses.Remove(course);
            await _context.SaveChangesAsync();

            return course;
        }

        private bool CourseExists(int id)
        {
            return _context.Courses.Any(e => e.Id == id);
        }
    }
}
