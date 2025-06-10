using Xunit;
using ImageConverter.WebApi.UseCases.ConvertImage;
using ImageConverter.WebApi.UseCases.UploadImage;
using ImageConverter.WebApi.UseCases.GetTaskStatus;
using NSubstitute;
using Amazon.S3;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using ImageConverter.WebApi.Configuration;
using ImageConverter.WebApi.Database;
using Microsoft.EntityFrameworkCore;

namespace ImageConverter.Tests;

public class ImageConversionTests
{
    private readonly IAmazonS3 _s3Client;
    private readonly AppDbContext _dbContext;
    private readonly IConvertImageHandler _convertImageHandler;
    private readonly IUploadImageHandler _uploadImageHandler;
    private readonly IGetTaskStatusHandler _getTaskStatusHandler;
    private readonly IOptions<S3Config> _s3Config;

    public ImageConversionTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDb")
            .Options;
        _dbContext = new AppDbContext(options);

        // Setup S3 mock
        _s3Client = Substitute.For<IAmazonS3>();
        
        // Setup S3 config
        _s3Config = Options.Create(new S3Config
        {
            ServiceUrl = "http://localhost:9000",
            AccessKey = "test",
            SecretKey = "test",
            BucketName = "test-bucket"
        });
        
        // Setup handlers
        _convertImageHandler = new ConvertImageHandler(_s3Client, _s3Config, _dbContext, Substitute.For<ILogger<ConvertImageHandler>>());
        _uploadImageHandler = new UploadImageHandler(_s3Client, _s3Config, _dbContext);
        _getTaskStatusHandler = new GetTaskStatusHandler(_dbContext);
    }

    [Fact]
    public async Task UploadImage_ValidImage_ReturnsSuccess()
    {
        // Arrange
        var imageData = new byte[] { 1, 2, 3, 4 };
        using var stream = new MemoryStream(imageData);

        // Act
        var result = await _uploadImageHandler.HandleAsync(new UploadImageRequest(stream), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(Guid.Empty, result.TaskId);
    }

    [Fact]
    public async Task GetTaskStatus_NonExistentTask_ThrowsException()
    {
        // Arrange
        var nonExistentTaskId = Guid.NewGuid();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _getTaskStatusHandler.HandleAsync(new GetTaskStatusRequest(nonExistentTaskId), CancellationToken.None));
        
        Assert.Equal($"Task {nonExistentTaskId} not found", exception.Message);
    }
} 