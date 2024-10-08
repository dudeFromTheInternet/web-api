using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;

namespace WebApi.MinimalApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : Controller
    {
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;

        public UsersController(IUserRepository userRepository, IMapper mapper)
        {
            _userRepository = userRepository;
            _mapper = mapper;
        }

        [HttpHead("{userId}")]
        [HttpGet("{userId}", Name = nameof(GetUserById))]
        [Produces("application/json", "application/xml")]
        public ActionResult<UserDto> GetUserById([FromRoute] Guid userId)
        {
            var user = _userRepository.FindById(userId);
            if (user == null)
            {
                return NotFound();
            }

            if (Request.HttpContext.Request.Method != "HEAD")
            {
                return Ok(_mapper.Map<UserDto>(user));
            }

            Response.ContentLength = 0;
            Response.ContentType = "application/json; charset=utf-8";
            return Ok();
        }


        [HttpPost]
        public IActionResult CreateUser([FromBody] PostUserDto? user)
        {
            if (user is null)
                return BadRequest();
            
            if (!ModelState.IsValid)
            {
                return UnprocessableEntity(ModelState);
            }

            if (!user.Login.All(char.IsLetterOrDigit))
            {
                ModelState.AddModelError("Login", "Логин должен содержать только буквы и цифры.");
                return UnprocessableEntity(ModelState);
            }

            var userEntity = _mapper.Map<UserEntity>(user);

            var createdUserEntity = _userRepository.Insert(userEntity);

            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = createdUserEntity.Id },
                createdUserEntity.Id);
        }

        [HttpPut("{userId}")]
        [Produces("application/json", "application/xml")]
        public IActionResult UpdateUser(Guid userId, [FromBody] UpdateUserDto? user)
        {
            if (user is null)
                return BadRequest();

            if (userId == Guid.Empty)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
                return UnprocessableEntity(ModelState);

            _userRepository.UpdateOrInsert(_mapper.Map(user, new UserEntity(userId)), out var isInserted);

            if (isInserted)
            {
                return CreatedAtRoute(
                    nameof(GetUserById),
                    new { userId },
                    userId
                );
            }

            return NoContent();
        }

        [HttpPatch("{userId}")]
        [Produces("application/json", "application/xml")]
        public IActionResult PartiallyUpdateUser([FromRoute] Guid userId, [FromBody] JsonPatchDocument<UpdateUserDto>? patchDoc)
        {
            if (patchDoc is null)
            {
                return BadRequest();
            }

            var user = new UpdateUserDto();
            patchDoc.ApplyTo(user, ModelState);
            if (userId == Guid.Empty)
            {
                return NotFound();
            }

            if (!TryValidateModel(user))
            {
                return UnprocessableEntity(ModelState);
            }

            var currentUser = _userRepository.FindById(userId);
            if (currentUser is null)
            {
                return NotFound();
            }

            _userRepository.Update(_mapper.Map(user, new UserEntity(userId)));
            return NoContent();
        }
        
        [HttpDelete("{userId}")]
        [Produces("application/json", "application/xml")]
        public IActionResult DeleteUser([FromRoute] Guid userId)
        {
            var currUser = _userRepository.FindById(userId);
            if (currUser is null)
            {
                return NotFound();
            }
        
            _userRepository.Delete(userId);
            return NoContent();
        }
        
        
        [HttpGet(Name = nameof(GetUsers))]
        [Produces("application/json", "application/xml")]
        public ActionResult<IEnumerable<UserDto>> GetUsers([FromServices] LinkGenerator linkGenerator,
            [FromQuery] int pageSize = 10, [FromQuery] int pageNumber = 1)
        {
            pageNumber = pageNumber < 1 ? 1 : pageNumber;
            pageSize = pageSize < 1 ? 1 : pageSize > 20 ? 20 : pageSize;
            
            var pageList = _userRepository.GetPage(pageNumber, pageSize);
            
            if (!pageList.Any())
            {
                return NotFound();
            }
            
            var users = _mapper.Map<IEnumerable<UserDto>>(pageList);
            
            var totalCount = pageList.TotalCount;
            var totalPages = pageList.TotalPages;
            
            var previousPageLink = pageList.HasPrevious
                ? linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetUsers), new { pageNumber = pageNumber - 1, pageSize })
                : null;

            var nextPageLink = pageList.HasNext
                ? linkGenerator.GetUriByRouteValues(HttpContext, nameof(GetUsers), new { pageNumber = pageNumber + 1, pageSize })
                : null;
            
            var paginationHeader = new
            {
                previousPageLink,
                nextPageLink,
                totalCount,
                pageSize,
                currentPage = pageNumber,
                totalPages
            };

            Response.Headers.Append("X-Pagination", JsonConvert.SerializeObject(paginationHeader));
            
            return Ok(users);
        }
        
        [HttpOptions]
        public IActionResult OptionsForUsers()
        {
            var allowedMethods = new[] { "GET", "POST", "OPTIONS" };
            Response.Headers.Append("Allow", allowedMethods);
            return Ok();
        }
    }
}
