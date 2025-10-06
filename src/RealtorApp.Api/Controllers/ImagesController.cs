using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RealtorApp.Domain.Interfaces;

namespace RealtorApp.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImagesController(IUserAuthService userAuthService) : RealtorApiBaseController
    {
        private readonly IUserAuthService _userAuthService = userAuthService;

        [HttpPost("v1/avatars/upload")]
        public async Task<IActionResult> UploadAvatar(IFormFile file)
        {
            await Task.CompletedTask;
            return Ok();
        }

        [HttpGet("v1/listings/{listingId}/avatars/{profileImageId}")]
        public async Task<IActionResult> GetAvatar([FromRoute] long profileImageId, [FromRoute] long listingId)
        {
            var isValid = await _userAuthService.UserIsConnectedToListing(RequiredCurrentUserId, listingId);

            if (!isValid)
            {
                return BadRequest();
            }

            await Task.CompletedTask;
            return File([], "image/png");
        }

        [HttpGet("v1/avatars/me")]
        public async Task<IActionResult> GetMyAvatar()
        {
            await Task.CompletedTask;
            return Ok();
        }
    }
}
