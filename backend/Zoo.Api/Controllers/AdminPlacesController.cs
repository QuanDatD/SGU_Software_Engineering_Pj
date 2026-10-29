using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zoo.BUS.DTOs;
using Zoo.BUS.Interfaces;
using Zoo.BUS.Services;
using Zoo.Api.Filters;

namespace Zoo.Api.Controllers;

[ApiController]
[Route("api/admin/places")]
[Authorize(Roles = "Admin")]
[AutoValidateAntiforgeryToken]
[ServiceFilter(typeof(ContentWriteFilter))]
public sealed class AdminPlacesController(IAdminPlaceService places, PlaceEditingService editing) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct) => Ok(await places.GetAllAsync(ct));
    public sealed class PlaceForm : SavePlaceRequest { public IFormFile? Image { get; set; } }
    [HttpPost]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public Task<IActionResult> Create([FromForm] PlaceForm form, CancellationToken ct) => Save(null, form, ct);
    [HttpPut("{id:guid}")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public Task<IActionResult> Update(Guid id, [FromForm] PlaceForm form, CancellationToken ct) => Save(id, form, ct);
    [HttpPatch("{id:guid}/hide")]
    public async Task<IActionResult> Hide(Guid id, CancellationToken ct)
        => await editing.HideAsync(id, ct) ? NoContent() : NotFound();

    [HttpPatch("{id:guid}/show")]
    public async Task<IActionResult> Show(Guid id, CancellationToken ct)
        => await editing.ShowAsync(id, ct) ? NoContent() : NotFound();

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await editing.DeleteAsync(id, ct);
        return result switch
        {
            DeletePlaceResult.NotFound    => NotFound(new { message = "Không tìm thấy điểm tham quan." }),
            DeletePlaceResult.HasRevisions => Conflict(new { message = "Không thể xóa điểm đang có phiên bản thuyết minh. Hãy dùng chức năng ẩn." }),
            _                              => NoContent()
        };
    }
    private async Task<IActionResult> Save(Guid? id, PlaceForm form, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(form.NameVi)) return BadRequest(new { message = "Cần nhập tên tiếng Việt." });
        await using var image = new MemoryStream();
        string? imageName = null;
        if (form.Image != null)
        {
            if (form.Image.Length <= 0 || form.Image.Length > 5 * 1024 * 1024)
                return BadRequest(new { message = "Ảnh phải nhỏ hơn hoặc bằng 5 MB." });
            await form.Image.CopyToAsync(image, ct);
            var bytes = image.ToArray();
            var extension = bytes.Length >= 8 && bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }) ? ".png"
                : bytes.Length >= 3 && bytes[0] == 255 && bytes[1] == 216 && bytes[2] == 255 ? ".jpg"
                : bytes.Length >= 12 && System.Text.Encoding.ASCII.GetString(bytes, 0, 4) == "RIFF"
                    && System.Text.Encoding.ASCII.GetString(bytes, 8, 4) == "WEBP" ? ".webp" : null;
            if (extension == null) return BadRequest(new { message = "Chỉ nhận ảnh PNG, JPEG, WebP hợp lệ." });
            imageName = "place" + extension; image.Position = 0;
        }
        var saved = await editing.SaveAsync(id, form, imageName, imageName == null ? null : image, ct);
        return saved == null ? NotFound() : Ok(new { id = saved, message = "Đã lưu. Tạo âm thanh và xuất bản bản nháp để cập nhật nội dung User." });
    }
}
