using CondoSphere.Data;
using CondoSphere.Data.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CondoSphere.API
{
    [Route("api/users")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class UsersApiController : ControllerBase
    {
        private readonly IUserRepository _userRepo;

        public UsersApiController(IUserRepository userRepo)
        {
            _userRepo = userRepo;
        }

        [HttpGet]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null, [FromQuery] string? sort = null)
        {
            var query = _userRepo.Query();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(u => u.FullName.Contains(search) || u.Email.Contains(search));

            query = sort switch
            {
                "name_desc" => query.OrderByDescending(u => u.FullName),
                "email" => query.OrderBy(u => u.Email),
                "email_desc" => query.OrderByDescending(u => u.Email),
                _ => query.OrderBy(u => u.FullName)
            };

            var total = await query.CountAsync();
            var users = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            return Ok(new { total, page, pageSize, data = users });
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Administrator")]
        public async Task<ActionResult<User>> GetById(string id)
        {
            var user = await _userRepo.GetByIdStringAsync(id);
            if (user == null) return NotFound();
            return Ok(user);
        }

        [HttpPost]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Create([FromBody] User user)
        {
            if (!ModelState.IsValid) return BadRequest(new ValidationProblemDetails(ModelState));
            await _userRepo.AddAsync(user);
            return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Update(string id, [FromBody] User user)
        {
            if (id != user.Id)
                return BadRequest(new ProblemDetails { Title = "ID mismatch", Status = 400 });

            _userRepo.Update(user);
            await _userRepo.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Delete(string id)
        {
            await _userRepo.DeleteByIdStringAsync(id);
            return NoContent();
        }
    }

}


