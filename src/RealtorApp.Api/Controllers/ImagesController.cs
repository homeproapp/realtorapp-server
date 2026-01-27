using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using RealtorApp.Domain.Constants;
using RealtorApp.Domain.Interfaces;
using RealtorApp.Domain.Settings;

namespace RealtorApp.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    [EnableRateLimiting(RateLimitConstants.Authenticated)]
    public class ImagesController(IUserAuthService userAuthService, IImagesService imagesService, AppSettings appSettings) : RealtorApiBaseController
    {
        private readonly IUserAuthService _userAuthService = userAuthService;
        private readonly IImagesService _imagesService = imagesService;
        private readonly AppSettings _appSettings = appSettings;

        [HttpPost("v1/avatars/upload")]
        public async Task<IActionResult> UploadAvatar(IFormFile file)
        {
            await Task.CompletedTask;
            return Ok();
        }

        [HttpGet("v1/listings/{listingId}/image/{fileId}")]
        public async Task<IActionResult> GetTaskImage([FromRoute] long listingId, [FromRoute] long fileId)
        {
            var isValid = await _userAuthService.UserIsConnectedToListing(RequiredCurrentUserId, listingId);

            if (!isValid)
            {
                return BadRequest();
            }

            var (ImageStream, ContentType) = await _imagesService.GetImageByFileIdAndListingIdAsync(fileId, listingId);

            if (ImageStream == null || ContentType == null)
            {
                return NotFound();
            }

            Response.Headers.CacheControl = $"public, max-age={_appSettings.ImageCacheDurationInSeconds}";
            Response.Headers.Expires = DateTime.UtcNow.AddSeconds(_appSettings.ImageCacheDurationInSeconds).ToString("R");

            return File(ImageStream, ContentType);
        }

        [HttpGet("v1/users/{userId}/avatars/{profileImageId}")]
        public async Task<IActionResult> GetAvatar([FromRoute] long profileImageId, [FromRoute] long userId)
        {
            var userToViewListingIds = await _userAuthService.GetUserToListingIdsByUserId(userId);

            var hasAccessToViewUserProfilePic = new List<bool>();

            foreach (var id in userToViewListingIds)
            {
                var isValid = await _userAuthService.UserIsConnectedToListing(RequiredCurrentUserId, id);

                hasAccessToViewUserProfilePic.Add(isValid);
            }

            // if they are assigned to 1 listing, grant access to view profile pic
            // otherwise, deny

            if (hasAccessToViewUserProfilePic.All(i => !i))
            {
                return BadRequest();
            }

            var (ImageStream, ContentType) = await _imagesService.GetImageByFileIdAsync(profileImageId);

            if (ImageStream == null || ContentType == null)
            {
                return NotFound();
            }

            Response.Headers.CacheControl = $"public, max-age={_appSettings.ImageCacheDurationInSeconds}";
            Response.Headers.Expires = DateTime.UtcNow.AddSeconds(_appSettings.ImageCacheDurationInSeconds).ToString("R");

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
