using AwesomeAssertions;
using JobApplicationTrackerAPI.Api.Models;

namespace JobApplicationTrackerAPI.UnitTests.Api;

/// <summary>
/// P1/M1g: the response envelope every endpoint returns. Small, pure,
/// and previously untested — the computed pagination properties especially.
/// </summary>
public class ApiModelTests
{
    [Fact]
    public void ApiResponse_Ok_SetsSuccessAndData()
    {
        var response = ApiResponse<string>.Ok("hello", "done");

        response.Success.Should().BeTrue();
        response.Data.Should().Be("hello");
        response.Message.Should().Be("done");
        response.Errors.Should().BeNull();
    }

    [Fact]
    public void ApiResponse_Created_UsesDefaultMessage()
    {
        var response = ApiResponse<Guid>.Created(Guid.NewGuid());

        response.Success.Should().BeTrue();
        response.Message.Should().Be("Resource created successfully.");
    }

    [Fact]
    public void ApiResponse_Fail_SetsErrorsAndNotSuccess()
    {
        var response = ApiResponse<object>.Fail("boom");

        response.Success.Should().BeFalse();
        response.Errors.Should().ContainSingle().Which.Should().Be("boom");
        response.Data.Should().BeNull();
    }

    [Fact]
    public void ApiResponse_Fail_WithList_SetsAllErrors()
    {
        var response = ApiResponse<object>.Fail(new List<string> { "a", "b" });

        response.Success.Should().BeFalse();
        response.Errors.Should().HaveCount(2);
    }

    [Fact]
    public void ApiPagination_TotalPages_RoundsUp()
    {
        var pagination = new ApiPagination(page: 1, pageSize: 10, totalCount: 25);

        pagination.TotalPages.Should().Be(3);
        pagination.HasNextPage.Should().BeTrue();
        pagination.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public void ApiPagination_LastPage_HasNoNextPage()
    {
        var pagination = new ApiPagination(page: 3, pageSize: 10, totalCount: 25);

        pagination.TotalPages.Should().Be(3);
        pagination.HasNextPage.Should().BeFalse();
        pagination.HasPreviousPage.Should().BeTrue();
    }

    [Fact]
    public void ApiPagination_ExactFit_HasSinglePage()
    {
        var pagination = new ApiPagination(page: 1, pageSize: 10, totalCount: 20);

        pagination.TotalPages.Should().Be(2);
        pagination.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public void ApiPagination_Empty_HasNoPages()
    {
        var pagination = new ApiPagination(page: 1, pageSize: 10, totalCount: 0);

        pagination.TotalPages.Should().Be(0);
        pagination.HasNextPage.Should().BeFalse();
        pagination.HasPreviousPage.Should().BeFalse();
    }
}
