using CondoSphere.Data.Interfaces;
using CondoSphere.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CondoSphere.API
{
    [Route("api/companies")]
    [ApiController]
    public class CompaniesApiController : ControllerBase
    {
        private readonly ICompanyRepository _repository;

        public CompaniesApiController(ICompanyRepository repository)
        {
            _repository = repository;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string search = null, [FromQuery] string sort = null)
        {
            var query = _repository.Query();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(c => c.Name.Contains(search) || c.Email.Contains(search));
            }

            query = sort switch
            {
                "name_desc" => query.OrderByDescending(c => c.Name),
                "email" => query.OrderBy(c => c.Email),
                "email_desc" => query.OrderByDescending(c => c.Email),
                _ => query.OrderBy(c => c.Name)
            };

            var total = await query.CountAsync();
            var companies = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            return Ok(new { total, page, pageSize, data = companies });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Company not found",
                    Detail = $"Company with id '{id}' does not exist.",
                    Status = StatusCodes.Status404NotFound
                });
            }
            return Ok(item);
        }

        [HttpPost]
        public async Task<IActionResult> Create(Company company)
        {
            await _repository.AddAsync(company);
            return CreatedAtAction(nameof(Get), new { id = company.Id }, company);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, Company company)
        {
            if (id != company.Id)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "ID mismatch",
                    Detail = "The id in the URL does not match the id in the body.",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            _repository.Update(company);
            await _repository.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var company = await _repository.GetByIdAsync(id);
            if (company == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Company not found",
                    Detail = $"Company with id '{id}' does not exist.",
                    Status = StatusCodes.Status404NotFound
                });
            }

            await _repository.DeleteAsync(id);
            return NoContent();
        }
    }

}

