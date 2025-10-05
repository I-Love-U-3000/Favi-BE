using Favi_BE.Interfaces;
using Favi_BE.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Favi_BE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProfilesController : ControllerBase
    {
        private readonly IProfileService _profiles;
        public ProfilesController(IProfileService profiles) => _profiles = profiles;


        [HttpGet("{id}")]
        public async Task<ActionResult<ProfileResponse>> GetById(Guid id) =>
            Ok(await _profiles.GetByIdAsync(id));

        //[Authorize]
        [HttpPut("{id}")]
        public async Task<ActionResult<ProfileResponse>> Update(Guid id, ProfileUpdateRequest dto)
        {
            var userId = Guid.Parse(User.FindFirst("sub")!.Value);
            if (userId != id) return Forbid();
            var updated = await _profiles.UpdateAsync(id, dto);
            return updated is null ? NotFound() : Ok(updated);
        }

        //[Authorize]
        [HttpPost("{id}/follow")]
        public async Task<IActionResult> Follow(Guid id)
        {
            var userId = Guid.Parse(User.FindFirst("sub")!.Value);
            var ok = await _profiles.FollowAsync(userId, id);
            return ok ? Ok() : BadRequest();
        }

        //[Authorize]
        [HttpDelete("{id}/follow")]
        public async Task<IActionResult> Unfollow(Guid id)
        {
            var userId = Guid.Parse(User.FindFirst("sub")!.Value);
            var ok = await _profiles.UnfollowAsync(userId, id);
            return ok ? Ok() : BadRequest();
        }

        [HttpGet("{id}/followers")]
        public async Task<IActionResult> Followers(Guid id) =>
            Ok(await _profiles.GetFollowersAsync(id));

        [HttpGet("{id}/followings")]
        public async Task<IActionResult> Followings(Guid id) =>
            Ok(await _profiles.GetFollowingsAsync(id));

        [HttpGet("{id}/links")]
        public async Task<IActionResult> GetLinks(Guid id) =>
            Ok(await _profiles.GetSocialLinksAsync(id));

        //[Authorize]
        [HttpPost("{id}/links")]
        public async Task<IActionResult> AddLink(Guid id, SocialLinkDto dto)
        {
            var userId = Guid.Parse(User.FindFirst("sub")!.Value);
            if (userId != id) return Forbid();
            return Ok(await _profiles.AddSocialLinkAsync(id, dto));
        }

        //[Authorize]
        [HttpDelete("{id}/links/{linkId}")]
        public async Task<IActionResult> RemoveLink(Guid id, Guid linkId)
        {
            var userId = Guid.Parse(User.FindFirst("sub")!.Value);
            if (userId != id) return Forbid();
            var ok = await _profiles.RemoveSocialLinkAsync(id, linkId);
            return ok ? Ok() : NotFound();
        }

        //[Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            if(id == Guid.Empty) return BadRequest("Invalid profile ID");
            if (await _profiles.DeleteAsync(id))
                return Ok();
            return BadRequest(); 
        }
    }
}