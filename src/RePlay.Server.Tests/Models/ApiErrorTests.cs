using FluentAssertions;
using Microsoft.AspNetCore.Http.HttpResults;
using RePlay.Server.Models;
using System.Text.Json;

namespace RePlay.Server.Tests.Models;

public class ApiErrorTests
{
    [Fact]
    public void ApiError_ShouldHaveRequiredProperties()
    {
        // Arrange & Act
        var error = new ApiError
        {
            Code = "TEST_ERROR",
            Message = "This is a test error"
        };

        // Assert
        error.Code.Should().Be("TEST_ERROR");
        error.Message.Should().Be("This is a test error");
        error.Details.Should().BeNull();
    }

    [Fact]
    public void ApiError_ShouldSerializeToJson()
    {
        // Arrange
        var error = new ApiError
        {
            Code = "TEST_ERROR",
            Message = "This is a test error",
            Details = "Additional details here"
        };

        // Act
        var json = JsonSerializer.Serialize(error);
        var deserialized = JsonSerializer.Deserialize<ApiError>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Code.Should().Be("TEST_ERROR");
        deserialized.Message.Should().Be("This is a test error");
        deserialized.Details.Should().Be("Additional details here");
    }

    [Fact]
    public void ApiErrorExtensions_BadRequest_ShouldReturnBadRequestWithApiError()
    {
        // Act
        var result = ApiErrorExtensions.BadRequest(
            "VALIDATION_ERROR",
            "Invalid input",
            "Username is required");

        // Assert
        result.Should().BeOfType<BadRequest<ApiError>>();
        var badRequest = (BadRequest<ApiError>)result;
        badRequest.Value.Should().NotBeNull();
        badRequest.Value!.Code.Should().Be("VALIDATION_ERROR");
        badRequest.Value.Message.Should().Be("Invalid input");
        badRequest.Value.Details.Should().Be("Username is required");
    }

    [Fact]
    public void ApiErrorExtensions_Unauthorized_ShouldReturn401WithApiError()
    {
        // Act
        var result = ApiErrorExtensions.Unauthorized(
            "NO_SESSION",
            "No active session found");

        // Assert
        result.Should().BeOfType<JsonHttpResult<ApiError>>();
        var jsonResult = (JsonHttpResult<ApiError>)result;
        jsonResult.StatusCode.Should().Be(401);
        jsonResult.Value.Should().NotBeNull();
        jsonResult.Value!.Code.Should().Be("NO_SESSION");
        jsonResult.Value.Message.Should().Be("No active session found");
    }

    [Fact]
    public void ApiErrorExtensions_InternalServerError_ShouldReturn500WithApiError()
    {
        // Act
        var result = ApiErrorExtensions.InternalServerError(
            "CONFIG_ERROR",
            "Configuration is invalid",
            "Missing ClientId");

        // Assert
        result.Should().BeOfType<JsonHttpResult<ApiError>>();
        var jsonResult = (JsonHttpResult<ApiError>)result;
        jsonResult.StatusCode.Should().Be(500);
        jsonResult.Value.Should().NotBeNull();
        jsonResult.Value!.Code.Should().Be("CONFIG_ERROR");
        jsonResult.Value.Message.Should().Be("Configuration is invalid");
        jsonResult.Value.Details.Should().Be("Missing ClientId");
    }

    [Fact]
    public void ApiErrorExtensions_ServiceUnavailable_ShouldReturn502WithApiError()
    {
        // Act
        var result = ApiErrorExtensions.ServiceUnavailable(
            "SPOTIFY_API_ERROR",
            "Failed to communicate with Spotify API",
            "Connection timeout");

        // Assert
        result.Should().BeOfType<JsonHttpResult<ApiError>>();
        var jsonResult = (JsonHttpResult<ApiError>)result;
        jsonResult.StatusCode.Should().Be(502);
        jsonResult.Value.Should().NotBeNull();
        jsonResult.Value!.Code.Should().Be("SPOTIFY_API_ERROR");
        jsonResult.Value.Message.Should().Be("Failed to communicate with Spotify API");
        jsonResult.Value.Details.Should().Be("Connection timeout");
    }

    [Fact]
    public void ApiError_ShouldSupportRecordEquality()
    {
        // Arrange
        var error1 = new ApiError
        {
            Code = "TEST_ERROR",
            Message = "Test message"
        };

        var error2 = new ApiError
        {
            Code = "TEST_ERROR",
            Message = "Test message"
        };

        var error3 = new ApiError
        {
            Code = "DIFFERENT_ERROR",
            Message = "Test message"
        };

        // Assert
        error1.Should().Be(error2);
        error1.Should().NotBe(error3);
    }
}
