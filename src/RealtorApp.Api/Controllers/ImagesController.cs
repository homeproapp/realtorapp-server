using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RealtorApp.Domain.Constants;
using RealtorApp.Domain.Interfaces;

namespace RealtorApp.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    [EnableRateLimiting(RateLimitConstants.Authenticated)]
    public class ImagesController(IUserAuthService userAuthService, IImagesService imagesService) : RealtorApiBaseController
    {
        private readonly IUserAuthService _userAuthService = userAuthService;
        private readonly IImagesService _imagesService = imagesService;

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

            var (ImageStream, ContentType) = await _imagesService.GetImageByFileIdAsync(profileImageId);

            if (ImageStream == null || ContentType == null)
            {
                return NotFound();
            }

            return File(ImageStream, ContentType);
        }

        [HttpGet("v1/avatars/me")]
        public async Task<IActionResult> GetMyAvatar()
        {
            var (ImageStream, ContentType) = await _imagesService.GetImageByUserIdAsync(RequiredCurrentUserId);

            if (ImageStream == null || ContentType == null)
            {
                return NotFound();
            }

            return File(ImageStream, ContentType);
        }
    }
}
