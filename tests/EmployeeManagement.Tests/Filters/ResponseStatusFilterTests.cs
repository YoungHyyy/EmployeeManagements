using EmployeeManagement.Api.Filters;
using EmployeeManagement.Application.DTOs;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;

namespace EmployeeManagement.Tests.Filters;

public class ResponseStatusFilterTests
{
    [Fact]
    public async Task AlignFilter_Keeps_201_When_Success_True()
    {
        var ctx = CreateResultExecutingContext(
            new ObjectResult(ApiResponse.Ok(new { id = 1 }))
            {
                StatusCode = StatusCodes.Status201Created
            });

        await RunAlignFilterAsync(ctx);

        var result = (ObjectResult)ctx.Result!;
        result.StatusCode.Should().Be(StatusCodes.Status201Created);
        ctx.HttpContext.Response.StatusCode.Should().Be(StatusCodes.Status201Created);
    }

    [Fact]
    public async Task AlignFilter_Keeps_404_When_Success_False()
    {
        var ctx = CreateResultExecutingContext(
            new ObjectResult(ApiResponse.Fail("Không tìm thấy nhân viên"))
            {
                StatusCode = StatusCodes.Status404NotFound
            });

        await RunAlignFilterAsync(ctx);

        var result = (ObjectResult)ctx.Result!;
        result.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task AlignFilter_Fixes_2xx_To_400_When_Success_False()
    {
        var ctx = CreateResultExecutingContext(
            new ObjectResult(ApiResponse.Fail("Sai mật khẩu"))
            {
                StatusCode = StatusCodes.Status200OK
            });

        await RunAlignFilterAsync(ctx);

        var result = (ObjectResult)ctx.Result!;
        result.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    /// <summary>
    /// Bug Swagger: login success nhưng mã vẫn 400 (lần fail trước / status lệch).
    /// </summary>
    [Fact]
    public async Task AlignFilter_Fixes_400_To_200_When_Success_True()
    {
        var ctx = CreateResultExecutingContext(
            new ObjectResult(new AuthResponse { Success = true, Message = "Đăng nhập thành công" })
            {
                StatusCode = StatusCodes.Status400BadRequest
            });

        await RunAlignFilterAsync(ctx);

        var result = (ObjectResult)ctx.Result!;
        result.StatusCode.Should().Be(StatusCodes.Status200OK);
        ctx.HttpContext.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task ApiResponseFilter_Wraps_Created_As_201_With_Success_True()
    {
        var objectResult = new ObjectResult(new { id = 5, name = "IT" })
        {
            StatusCode = StatusCodes.Status201Created
        };
        var ctx = CreateResultExecutingContext(objectResult);

        await new ApiResponseFilter().OnResultExecutionAsync(ctx, () =>
            Task.FromResult(CreateResultExecutedContext(ctx)));

        var result = (ObjectResult)ctx.Result!;
        result.StatusCode.Should().Be(StatusCodes.Status201Created);
        var body = result.Value.Should().BeOfType<ApiResponse<object?>>().Subject;
        body.Success.Should().BeTrue();
        body.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task ApiResponseFilter_Infers_400_When_Fail_Without_StatusCode()
    {
        var ctx = CreateResultExecutingContext(new ObjectResult(ApiResponse.Fail("lỗi")));
        await new ApiResponseFilter().OnResultExecutionAsync(ctx, () =>
            Task.FromResult(CreateResultExecutedContext(ctx)));

        var result = (ObjectResult)ctx.Result!;
        result.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task ApiResponseFilter_Keeps_NotFound_Fail_As_404()
    {
        var ctx = CreateResultExecutingContext(
            new NotFoundObjectResult(ApiResponse.Fail("Không tìm thấy")));
        await new ApiResponseFilter().OnResultExecutionAsync(ctx, () =>
            Task.FromResult(CreateResultExecutedContext(ctx)));

        var result = (ObjectResult)ctx.Result!;
        result.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        var body = result.Value.Should().BeAssignableTo<ApiResponseBase>().Subject;
        ReadSuccess(body).Should().BeFalse();
    }

    [Fact]
    public async Task ApiResponseFilter_Fixes_AuthSuccess_Stuck_At_400()
    {
        // Giống bug: Ok(AuthResponse success) bị ObjectResult StatusCode=400
        var ctx = CreateResultExecutingContext(
            new ObjectResult(new AuthResponse
            {
                Success = true,
                Message = "Đăng nhập thành công",
                AccessToken = "tok"
            })
            {
                StatusCode = StatusCodes.Status400BadRequest
            });

        await new ApiResponseFilter().OnResultExecutionAsync(ctx, () =>
            Task.FromResult(CreateResultExecutedContext(ctx)));

        var result = (ObjectResult)ctx.Result!;
        result.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task Pipeline_Fail_Then_Success_Status_Changes()
    {
        // Lần 1: login fail → 400
        var failCtx = CreateResultExecutingContext(
            new BadRequestObjectResult(ApiResponse.Fail("Thông tin đăng nhập không hợp lệ")));
        await RunFullPipelineAsync(failCtx);
        ((ObjectResult)failCtx.Result!).StatusCode.Should().Be(400);

        // Lần 2: cùng API login OK → 200 (không kẹt 400)
        var okCtx = CreateResultExecutingContext(
            new OkObjectResult(new AuthResponse
            {
                Success = true,
                Message = "Đăng nhập thành công",
                AccessToken = "abc"
            }));
        await RunFullPipelineAsync(okCtx);
        ((ObjectResult)okCtx.Result!).StatusCode.Should().Be(200);
        okCtx.HttpContext.Response.StatusCode.Should().Be(200);
    }

    private static Task RunAlignFilterAsync(ResultExecutingContext ctx)
        => new ResponseStatusAlignFilter().OnResultExecutionAsync(
            ctx,
            () => Task.FromResult(CreateResultExecutedContext(ctx)));

    private static async Task RunFullPipelineAsync(ResultExecutingContext ctx)
    {
        await new ApiResponseFilter().OnResultExecutionAsync(ctx, () =>
            Task.FromResult(CreateResultExecutedContext(ctx)));
        await new ResponseStatusAlignFilter().OnResultExecutionAsync(ctx, () =>
            Task.FromResult(CreateResultExecutedContext(ctx)));
    }

    private static bool ReadSuccess(ApiResponseBase value)
    {
        var prop = value.GetType().GetProperty("Success");
        return prop?.GetValue(value) is bool b && b;
    }

    private static ResultExecutingContext CreateResultExecutingContext(IActionResult result)
    {
        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        return new ResultExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            result,
            controller: new object());
    }

    private static ResultExecutedContext CreateResultExecutedContext(ResultExecutingContext ctx)
        => new(ctx, ctx.Filters.ToList(), ctx.Result!, ctx.Controller);
}
