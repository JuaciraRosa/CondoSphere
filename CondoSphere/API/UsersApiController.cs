using CondoSphere.Data;
using CondoSphere.Data.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CondoSphere.API
{
    [Route("api/users")]
    [ApiController]
    public class UsersApiController : ControllerBase
    {
        private readonly IUserRepository _userRepo;

        public UsersApiController(IUserRepository userRepo)
        {
            _userRepo = userRepo;
        }
        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetAll()
        {
            var users = await _userRepo.GetAllAsync();
            return Ok(users);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetById(int id)
        {
            var user = await _userRepo.GetByIdAsync(id);
            if (user == null) return NotFound();
            return Ok(user);
        }

        [HttpPost]
        public async Task<ActionResult<User>> Create(User user)
        {
            await _userRepo.AddAsync(user);
            return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, User user)
        {
            if (id != user.Id)
                return BadRequest();

            _userRepo.Update(user);
            await _userRepo.SaveChangesAsync();
            return NoContent();
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _userRepo.DeleteAsync(id);
            return NoContent();
        }
    }
}


