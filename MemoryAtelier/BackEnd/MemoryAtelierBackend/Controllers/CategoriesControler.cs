using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MemoryAtelierBackend.DTOs;
using MemoryAtelierBackend.Services;

namespace MemoryAtelierBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController(CategoryService categoryService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await categoryService.GetTreeAsync(User.IsInRole("Admin")));

    [HttpGet("deleted")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetDeleted() =>
        Ok(await categoryService.GetDeletedAsync());

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateCategoryDto dto)
    {
        var cat = await categoryService.CreateAsync(dto);
        return Ok(cat);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCategoryDto dto)
    {
        var (cat, error) = await categoryService.UpdateAsync(id, dto);
        if (error != null) return BadRequest(new { message = error });
        return cat == null ? NotFound() : Ok(cat);
    }

    [HttpPut("reorder")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Reorder([FromBody] ReorderCategoriesDto dto)
    {
        await categoryService.ReorderAsync(dto);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var success = await categoryService.DeleteAsync(id);
        return success ? NoContent() : NotFound();
    }

    [HttpPost("{id:guid}/restore")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Restore(Guid id)
    {
        var success = await categoryService.RestoreAsync(id);
        return success ? NoContent() : NotFound();
    }

    [HttpPatch("{id:guid}/hidden")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SetHidden(Guid id, [FromBody] SetHiddenDto dto)
    {
        var success = dto.Hidden ? await categoryService.HideAsync(id) : await categoryService.ShowAsync(id);
        return success ? NoContent() : NotFound();
    }

    [HttpDelete("{id:guid}/purge")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Purge(Guid id)
    {
        var result = await categoryService.PurgeAsync(id);
        return result switch
        {
            PurgeResult.Purged => NoContent(),
            PurgeResult.HasReferences => Conflict(new { message = "Категорията все още има подкатегории и не може да бъде изтрита завинаги." }),
            _ => NotFound()
        };
    }
}