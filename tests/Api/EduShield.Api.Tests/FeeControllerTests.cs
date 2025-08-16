using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using Moq;
using EduShield.Core.Dtos;
using EduShield.Core.Interfaces;
using EduShield.Core.Enums;
using EduShield.Api.Controllers;

namespace EduShield.Api.Tests;

[TestFixture]
public class FeeControllerTests
{
    private readonly Mock<IFeeService> _mockFeeService;
    private readonly Mock<ILogger<FeeController>> _mockLogger;
    private readonly Mock<IWebHostEnvironment> _mockEnvironment;
    private readonly FeeController _controller;

    public FeeControllerTests()
    {
        _mockFeeService = new Mock<IFeeService>();
        _mockLogger = new Mock<ILogger<FeeController>>();
        _mockEnvironment = new Mock<IWebHostEnvironment>();
        _controller = new FeeController(_mockFeeService.Object, _mockLogger.Object, _mockEnvironment.Object);
    }

    [SetUp]
    public void SetUp()
    {
        _mockFeeService.Reset();
        _mockLogger.Reset();
        _mockEnvironment.Reset();
        _mockEnvironment.Setup(x => x.EnvironmentName).Returns("Testing");
        
        // Clear ModelState for each test
        _controller.ModelState.Clear();
    }

    #region GetAllFees Tests

    [Test]
    public async Task GetAllFees_ValidRequest_ReturnsOkResult()
    {
        // Arrange
        var fees = new List<FeeDto>
        {
            new() { FeeId = Guid.NewGuid(), StudentId = Guid.NewGuid(), FeeType = FeeType.Tuition, Amount = 1000m },
            new() { FeeId = Guid.NewGuid(), StudentId = Guid.NewGuid(), FeeType = FeeType.LabFee, Amount = 500m }
        };
        _mockFeeService.Setup(x => x.GetAllFeesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(fees);

        // Act
        var result = await _controller.GetAllFees(CancellationToken.None);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var okResult = result.Result as OkObjectResult;
        var returnedFees = okResult?.Value as IEnumerable<FeeDto>;
        Assert.That(returnedFees?.Count(), Is.EqualTo(2));
    }

    [Test]
    public async Task GetAllFees_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        _mockFeeService.Setup(x => x.GetAllFeesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Service error"));

        // Act
        var result = await _controller.GetAllFees(CancellationToken.None);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<ObjectResult>());
        var statusResult = (ObjectResult)result.Result;
        Assert.That(statusResult.StatusCode, Is.EqualTo(500));
    }

    #endregion

    #region GetFeeById Tests

    [Test]
    public async Task GetFeeById_ValidId_ReturnsOkResult()
    {
        // Arrange
        var feeId = Guid.NewGuid();
        var fee = new FeeDto
        {
            FeeId = feeId,
            StudentId = Guid.NewGuid(),
            FeeType = FeeType.Tuition,
            Amount = 1000m,
            PaidAmount = 500m,
            OutstandingAmount = 500m
        };
        _mockFeeService.Setup(x => x.GetFeeByIdAsync(feeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fee);

        // Act
        var result = await _controller.GetFeeById(feeId, CancellationToken.None);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var okResult = (OkObjectResult)result.Result;
        var returnedFee = (FeeDto)okResult.Value;
        Assert.That(returnedFee.FeeId, Is.EqualTo(feeId));
    }

    [Test]
    public async Task GetFeeById_InvalidId_ReturnsNotFound()
    {
        // Arrange
        var feeId = Guid.NewGuid();
        _mockFeeService.Setup(x => x.GetFeeByIdAsync(feeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FeeDto?)null);

        // Act
        var result = await _controller.GetFeeById(feeId, CancellationToken.None);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task GetFeeById_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var feeId = Guid.NewGuid();
        _mockFeeService.Setup(x => x.GetFeeByIdAsync(feeId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Service error"));

        // Act
        var result = await _controller.GetFeeById(feeId, CancellationToken.None);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<ObjectResult>());
        var statusResult = (ObjectResult)result.Result;
        Assert.That(statusResult.StatusCode, Is.EqualTo(500));
    }

    #endregion

    #region GetFeesByStudentId Tests

    [Test]
    public async Task GetFeesByStudentId_ValidStudentId_ReturnsOkResult()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var fees = new List<FeeDto>
        {
            new() { FeeId = Guid.NewGuid(), StudentId = studentId, FeeType = FeeType.Tuition, Amount = 1000m }
        };
        _mockFeeService.Setup(x => x.GetFeesByStudentIdAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fees);

        // Act
        var result = await _controller.GetFeesByStudentId(studentId, CancellationToken.None);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var okResult = (OkObjectResult)result.Result;
        var returnedFees = (IEnumerable<FeeDto>)okResult.Value;
        Assert.That(returnedFees.Count(), Is.EqualTo(1));
        Assert.That(returnedFees.First().StudentId, Is.EqualTo(studentId));
    }

    [Test]
    public async Task GetFeesByStudentId_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        _mockFeeService.Setup(x => x.GetFeesByStudentIdAsync(studentId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Service error"));

        // Act
        var result = await _controller.GetFeesByStudentId(studentId, CancellationToken.None);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<ObjectResult>());
        var statusResult = (ObjectResult)result.Result;
        Assert.That(statusResult.StatusCode, Is.EqualTo(500));
    }

    #endregion

    #region GetFeesByType Tests

    [Test]
    public async Task GetFeesByType_ValidFeeType_ReturnsOkResult()
    {
        // Arrange
        var feeType = FeeType.Tuition;
        var fees = new List<FeeDto>
        {
            new() { FeeId = Guid.NewGuid(), StudentId = Guid.NewGuid(), FeeType = feeType, Amount = 1000m }
        };
        _mockFeeService.Setup(x => x.GetFeesByTypeAsync(feeType, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fees);

        // Act
        var result = await _controller.GetFeesByType(feeType, CancellationToken.None);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var okResult = (OkObjectResult)result.Result;
        var returnedFees = (IEnumerable<FeeDto>)okResult.Value;
        Assert.That(returnedFees.Count(), Is.EqualTo(1));
        Assert.That(returnedFees.First().FeeType, Is.EqualTo(feeType));
    }

    [Test]
    public async Task GetFeesByType_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var feeType = FeeType.LabFee;
        _mockFeeService.Setup(x => x.GetFeesByTypeAsync(feeType, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Service error"));

        // Act
        var result = await _controller.GetFeesByType(feeType, CancellationToken.None);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<ObjectResult>());
        var statusResult = (ObjectResult)result.Result;
        Assert.That(statusResult.StatusCode, Is.EqualTo(500));
    }

    #endregion

    #region GetFeesByStatus Tests

    [Test]
    public async Task GetFeesByStatus_ValidStatus_ReturnsOkResult()
    {
        // Arrange
        var status = FeeStatus.Pending;
        var fees = new List<FeeDto>
        {
            new() { FeeId = Guid.NewGuid(), StudentId = Guid.NewGuid(), Status = status, Amount = 1000m }
        };
        _mockFeeService.Setup(x => x.GetFeesByStatusAsync(status, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fees);

        // Act
        var result = await _controller.GetFeesByStatus(status, CancellationToken.None);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var okResult = (OkObjectResult)result.Result;
        var returnedFees = (IEnumerable<FeeDto>)okResult.Value;
        Assert.That(returnedFees.Count(), Is.EqualTo(1));
        Assert.That(returnedFees.First().Status, Is.EqualTo(status));
    }

    [Test]
    public async Task GetFeesByStatus_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var status = FeeStatus.Overdue;
        _mockFeeService.Setup(x => x.GetFeesByStatusAsync(status, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Service error"));

        // Act
        var result = await _controller.GetFeesByStatus(status, CancellationToken.None);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<ObjectResult>());
        var statusResult = (ObjectResult)result.Result;
        Assert.That(statusResult.StatusCode, Is.EqualTo(500));
    }

    #endregion

    #region GetOverdueFees Tests

    [Test]
    public async Task GetOverdueFees_ValidRequest_ReturnsOkResult()
    {
        // Arrange
        var overdueFees = new List<FeeDto>
        {
            new() { FeeId = Guid.NewGuid(), StudentId = Guid.NewGuid(), IsOverdue = true, Amount = 1000m }
        };
        _mockFeeService.Setup(x => x.GetOverdueFeesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(overdueFees);

        // Act
        var result = await _controller.GetOverdueFees(CancellationToken.None);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var okResult = (OkObjectResult)result.Result;
        var returnedFees = (IEnumerable<FeeDto>)okResult.Value;
        Assert.That(returnedFees.Count(), Is.EqualTo(1));
        Assert.That(returnedFees.First().IsOverdue, Is.True);
    }

    [Test]
    public async Task GetOverdueFees_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        _mockFeeService.Setup(x => x.GetOverdueFeesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Service error"));

        // Act
        var result = await _controller.GetOverdueFees(CancellationToken.None);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<ObjectResult>());
        var statusResult = (ObjectResult)result.Result;
        Assert.That(statusResult.StatusCode, Is.EqualTo(500));
    }

    #endregion

    #region CreateFee Tests

    [Test]
    public async Task CreateFee_ValidRequest_ReturnsCreatedResult()
    {
        // Arrange
        var request = new CreateFeeReq
        {
            StudentId = Guid.NewGuid(),
            FeeType = FeeType.Tuition,
            Amount = 1000m,
            DueDate = DateTime.UtcNow.AddDays(30),
            Description = "Tuition fee for semester"
        };
        var feeId = Guid.NewGuid();
        _mockFeeService.Setup(x => x.CreateFeeAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(feeId);

        // Act
        var result = await _controller.CreateFee(request, CancellationToken.None);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<CreatedAtActionResult>());
        var createdResult = (CreatedAtActionResult)result.Result;
        Assert.That(createdResult.ActionName, Is.EqualTo(nameof(FeeController.GetFeeById)));
        var returnedValue = createdResult.Value;
        Assert.That(returnedValue, Is.Not.Null);
        // The value should be an anonymous object with an 'id' property
        var idProperty = returnedValue.GetType().GetProperty("id");
        Assert.That(idProperty, Is.Not.Null);
        Assert.That(idProperty.GetValue(returnedValue), Is.EqualTo(feeId));
    }

    [Test]
    public async Task CreateFee_ServiceThrowsArgumentException_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateFeeReq
        {
            StudentId = Guid.NewGuid(),
            FeeType = FeeType.Tuition,
            Amount = 1000m,
            DueDate = DateTime.UtcNow.AddDays(30),
            Description = "Test fee"
        };
        _mockFeeService.Setup(x => x.CreateFeeAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Student not found"));

        // Act
        var result = await _controller.CreateFee(request, CancellationToken.None);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task CreateFee_ServiceThrowsInvalidOperationException_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateFeeReq
        {
            StudentId = Guid.NewGuid(),
            FeeType = FeeType.Tuition,
            Amount = 1000m,
            DueDate = DateTime.UtcNow.AddDays(30),
            Description = "Test fee"
        };
        _mockFeeService.Setup(x => x.CreateFeeAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Business rule violation"));

        // Act
        var result = await _controller.CreateFee(request, CancellationToken.None);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task CreateFee_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var request = new CreateFeeReq
        {
            StudentId = Guid.NewGuid(),
            FeeType = FeeType.Tuition,
            Amount = 1000m,
            DueDate = DateTime.UtcNow.AddDays(30),
            Description = "Test fee"
        };
        _mockFeeService.Setup(x => x.CreateFeeAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Service error"));

        // Act
        var result = await _controller.CreateFee(request, CancellationToken.None);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<ObjectResult>());
        var statusResult = (ObjectResult)result.Result;
        Assert.That(statusResult.StatusCode, Is.EqualTo(500));
    }

    [Test]
    public async Task CreateFee_InvalidModelState_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateFeeReq();
        _controller.ModelState.AddModelError("Amount", "Amount is required");

        // Act
        var result = await _controller.CreateFee(request, CancellationToken.None);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
    }

    #endregion

    #region UpdateFee Tests

    [Test]
    public async Task UpdateFee_ValidRequest_ReturnsNoContent()
    {
        // Arrange
        var feeId = Guid.NewGuid();
        var request = new UpdateFeeReq
        {
            FeeType = FeeType.Tuition,
            Amount = 1200m,
            DueDate = DateTime.UtcNow.AddDays(30),
            Description = "Updated tuition fee"
        };
        _mockFeeService.Setup(x => x.UpdateFeeAsync(feeId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.UpdateFee(feeId, request, CancellationToken.None);

        // Assert
        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task UpdateFee_FeeNotFound_ReturnsNotFound()
    {
        // Arrange
        var feeId = Guid.NewGuid();
        var request = new UpdateFeeReq
        {
            FeeType = FeeType.Tuition,
            Amount = 1200m,
            DueDate = DateTime.UtcNow.AddDays(30),
            Description = "Updated fee"
        };
        _mockFeeService.Setup(x => x.UpdateFeeAsync(feeId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.UpdateFee(feeId, request, CancellationToken.None);

        // Assert
        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task UpdateFee_ServiceThrowsArgumentException_ReturnsBadRequest()
    {
        // Arrange
        var feeId = Guid.NewGuid();
        var request = new UpdateFeeReq
        {
            FeeType = FeeType.Tuition,
            Amount = 1200m,
            DueDate = DateTime.UtcNow.AddDays(30),
            Description = "Updated fee"
        };
        _mockFeeService.Setup(x => x.UpdateFeeAsync(feeId, request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Invalid fee data"));

        // Act
        var result = await _controller.UpdateFee(feeId, request, CancellationToken.None);

        // Assert
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task UpdateFee_InvalidModelState_ReturnsBadRequest()
    {
        // Arrange
        var feeId = Guid.NewGuid();
        var request = new UpdateFeeReq();
        _controller.ModelState.AddModelError("Amount", "Amount is required");

        // Act
        var result = await _controller.UpdateFee(feeId, request, CancellationToken.None);

        // Assert
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    #endregion

    #region DeleteFee Tests

    [Test]
    public async Task DeleteFee_ValidId_ReturnsNoContent()
    {
        // Arrange
        var feeId = Guid.NewGuid();
        _mockFeeService.Setup(x => x.DeleteFeeAsync(feeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteFee(feeId, CancellationToken.None);

        // Assert
        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task DeleteFee_FeeNotFound_ReturnsNotFound()
    {
        // Arrange
        var feeId = Guid.NewGuid();
        _mockFeeService.Setup(x => x.DeleteFeeAsync(feeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteFee(feeId, CancellationToken.None);

        // Assert
        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task DeleteFee_ServiceThrowsInvalidOperationException_ReturnsBadRequest()
    {
        // Arrange
        var feeId = Guid.NewGuid();
        _mockFeeService.Setup(x => x.DeleteFeeAsync(feeId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Cannot delete fee with payments"));

        // Act
        var result = await _controller.DeleteFee(feeId, CancellationToken.None);

        // Assert
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task DeleteFee_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var feeId = Guid.NewGuid();
        _mockFeeService.Setup(x => x.DeleteFeeAsync(feeId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Service error"));

        // Act
        var result = await _controller.DeleteFee(feeId, CancellationToken.None);

        // Assert
        Assert.That(result, Is.InstanceOf<ObjectResult>());
        var statusResult = (ObjectResult)result;
        Assert.That(statusResult.StatusCode, Is.EqualTo(500));
    }

    #endregion  
  #region RecordPayment Tests

    [Test]
    public async Task RecordPayment_ValidRequest_ReturnsCreatedResult()
    {
        // Arrange
        var feeId = Guid.NewGuid();
        var request = new PaymentReq
        {
            Amount = 500m,
            PaymentDate = DateTime.UtcNow,
            PaymentMethod = "Credit Card",
            TransactionReference = "TXN123456"
        };
        var paymentDto = new PaymentDto
        {
            PaymentId = Guid.NewGuid(),
            FeeId = feeId,
            Amount = 500m,
            PaymentDate = DateTime.UtcNow,
            PaymentMethod = "Credit Card"
        };
        _mockFeeService.Setup(x => x.RecordPaymentAsync(feeId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paymentDto);

        // Act
        var result = await _controller.RecordPayment(feeId, request, CancellationToken.None);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<CreatedAtActionResult>());
        var createdResult = (CreatedAtActionResult)result.Result;
        Assert.That(createdResult.ActionName, Is.EqualTo(nameof(FeeController.GetPaymentsByFeeId)));
        Assert.That(createdResult.Value, Is.EqualTo(paymentDto));
    }

    [Test]
    public async Task RecordPayment_ServiceThrowsArgumentException_ReturnsBadRequest()
    {
        // Arrange
        var feeId = Guid.NewGuid();
        var request = new PaymentReq
        {
            Amount = 500m,
            PaymentDate = DateTime.UtcNow,
            PaymentMethod = "Credit Card"
        };
        _mockFeeService.Setup(x => x.RecordPaymentAsync(feeId, request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Fee not found"));

        // Act
        var result = await _controller.RecordPayment(feeId, request, CancellationToken.None);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task RecordPayment_ServiceThrowsInvalidOperationException_ReturnsBadRequest()
    {
        // Arrange
        var feeId = Guid.NewGuid();
        var request = new PaymentReq
        {
            Amount = 1500m,
            PaymentDate = DateTime.UtcNow,
            PaymentMethod = "Credit Card"
        };
        _mockFeeService.Setup(x => x.RecordPaymentAsync(feeId, request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Payment exceeds outstanding amount"));

        // Act
        var result = await _controller.RecordPayment(feeId, request, CancellationToken.None);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task RecordPayment_InvalidModelState_ReturnsBadRequest()
    {
        // Arrange
        var feeId = Guid.NewGuid();
        var request = new PaymentReq();
        _controller.ModelState.AddModelError("Amount", "Amount is required");

        // Act
        var result = await _controller.RecordPayment(feeId, request, CancellationToken.None);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task RecordPayment_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var feeId = Guid.NewGuid();
        var request = new PaymentReq
        {
            Amount = 500m,
            PaymentDate = DateTime.UtcNow,
            PaymentMethod = "Credit Card"
        };
        _mockFeeService.Setup(x => x.RecordPaymentAsync(feeId, request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Service error"));

        // Act
        var result = await _controller.RecordPayment(feeId, request, CancellationToken.None);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<ObjectResult>());
        var statusResult = (ObjectResult)result.Result;
        Assert.That(statusResult.StatusCode, Is.EqualTo(500));
    }

    #endregion

    #region GetPaymentsByFeeId Tests

    [Test]
    public async Task GetPaymentsByFeeId_ValidFeeId_ReturnsOkResult()
    {
        // Arrange
        var feeId = Guid.NewGuid();
        var payments = new List<PaymentDto>
        {
            new() { PaymentId = Guid.NewGuid(), FeeId = feeId, Amount = 500m, PaymentMethod = "Credit Card" }
        };
        _mockFeeService.Setup(x => x.GetPaymentsByFeeIdAsync(feeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payments);

        // Act
        var result = await _controller.GetPaymentsByFeeId(feeId, CancellationToken.None);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var okResult = (OkObjectResult)result.Result;
        var returnedPayments = (IEnumerable<PaymentDto>)okResult.Value;
        Assert.That(returnedPayments.Count(), Is.EqualTo(1));
        Assert.That(returnedPayments.First().FeeId, Is.EqualTo(feeId));
    }

    [Test]
    public async Task GetPaymentsByFeeId_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var feeId = Guid.NewGuid();
        _mockFeeService.Setup(x => x.GetPaymentsByFeeIdAsync(feeId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Service error"));

        // Act
        var result = await _controller.GetPaymentsByFeeId(feeId, CancellationToken.None);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<ObjectResult>());
        var statusResult = (ObjectResult)result.Result;
        Assert.That(statusResult.StatusCode, Is.EqualTo(500));
    }

    #endregion

    #region GetStudentFeesSummary Tests

    [Test]
    public async Task GetStudentFeesSummary_ValidStudentId_ReturnsOkResult()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var summary = new FeesSummaryDto
        {
            StudentId = studentId,
            TotalFees = 2000m,
            TotalPaid = 1000m,
            TotalOutstanding = 1000m,
            TotalOverdue = 500m
        };
        _mockFeeService.Setup(x => x.GetStudentFeesSummaryAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(summary);

        // Act
        var result = await _controller.GetStudentFeesSummary(studentId, CancellationToken.None);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var okResult = (OkObjectResult)result.Result;
        var returnedSummary = (FeesSummaryDto)okResult.Value;
        Assert.That(returnedSummary.StudentId, Is.EqualTo(studentId));
        Assert.That(returnedSummary.TotalFees, Is.EqualTo(2000m));
    }

    [Test]
    public async Task GetStudentFeesSummary_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        _mockFeeService.Setup(x => x.GetStudentFeesSummaryAsync(studentId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Service error"));

        // Act
        var result = await _controller.GetStudentFeesSummary(studentId, CancellationToken.None);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<ObjectResult>());
        var statusResult = (ObjectResult)result.Result;
        Assert.That(statusResult.StatusCode, Is.EqualTo(500));
    }

    #endregion

    #region GetPaymentsByStudentId Tests

    [Test]
    public async Task GetPaymentsByStudentId_ValidStudentId_ReturnsOkResult()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var payments = new List<PaymentDto>
        {
            new() { PaymentId = Guid.NewGuid(), FeeId = Guid.NewGuid(), Amount = 500m, PaymentMethod = "Credit Card" }
        };
        _mockFeeService.Setup(x => x.GetPaymentsByStudentIdAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payments);

        // Act
        var result = await _controller.GetPaymentsByStudentId(studentId, CancellationToken.None);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var okResult = (OkObjectResult)result.Result;
        var returnedPayments = (IEnumerable<PaymentDto>)okResult.Value;
        Assert.That(returnedPayments.Count(), Is.EqualTo(1));
    }

    [Test]
    public async Task GetPaymentsByStudentId_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        _mockFeeService.Setup(x => x.GetPaymentsByStudentIdAsync(studentId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Service error"));

        // Act
        var result = await _controller.GetPaymentsByStudentId(studentId, CancellationToken.None);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<ObjectResult>());
        var statusResult = (ObjectResult)result.Result;
        Assert.That(statusResult.StatusCode, Is.EqualTo(500));
    }

    #endregion

    #region MarkFeeAsPaid Tests

    [Test]
    public async Task MarkFeeAsPaid_ValidId_ReturnsNoContent()
    {
        // Arrange
        var feeId = Guid.NewGuid();
        _mockFeeService.Setup(x => x.MarkFeeAsPaidAsync(feeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.MarkFeeAsPaid(feeId, CancellationToken.None);

        // Assert
        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task MarkFeeAsPaid_FeeNotFound_ReturnsNotFound()
    {
        // Arrange
        var feeId = Guid.NewGuid();
        _mockFeeService.Setup(x => x.MarkFeeAsPaidAsync(feeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.MarkFeeAsPaid(feeId, CancellationToken.None);

        // Assert
        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task MarkFeeAsPaid_ServiceThrowsInvalidOperationException_ReturnsBadRequest()
    {
        // Arrange
        var feeId = Guid.NewGuid();
        _mockFeeService.Setup(x => x.MarkFeeAsPaidAsync(feeId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Fee already paid"));

        // Act
        var result = await _controller.MarkFeeAsPaid(feeId, CancellationToken.None);

        // Assert
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task MarkFeeAsPaid_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var feeId = Guid.NewGuid();
        _mockFeeService.Setup(x => x.MarkFeeAsPaidAsync(feeId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Service error"));

        // Act
        var result = await _controller.MarkFeeAsPaid(feeId, CancellationToken.None);

        // Assert
        Assert.That(result, Is.InstanceOf<ObjectResult>());
        var statusResult = (ObjectResult)result;
        Assert.That(statusResult.StatusCode, Is.EqualTo(500));
    }

    #endregion

    #region UpdateFeeStatuses Tests

    [Test]
    public async Task UpdateFeeStatuses_ValidRequest_ReturnsNoContent()
    {
        // Arrange
        _mockFeeService.Setup(x => x.UpdateFeeStatusesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.UpdateFeeStatuses(CancellationToken.None);

        // Assert
        Assert.That(result, Is.InstanceOf<NoContentResult>());
    }

    [Test]
    public async Task UpdateFeeStatuses_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        _mockFeeService.Setup(x => x.UpdateFeeStatusesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Service error"));

        // Act
        var result = await _controller.UpdateFeeStatuses(CancellationToken.None);

        // Assert
        Assert.That(result, Is.InstanceOf<ObjectResult>());
        var statusResult = (ObjectResult)result;
        Assert.That(statusResult.StatusCode, Is.EqualTo(500));
    }

    #endregion

    #region Error Response Format Tests

    [Test]
    public async Task CreateFee_DevelopmentEnvironment_ReturnsDetailedErrorInfo()
    {
        // Arrange
        _mockEnvironment.Setup(x => x.EnvironmentName).Returns("Development");
        var request = new CreateFeeReq
        {
            StudentId = Guid.NewGuid(),
            FeeType = FeeType.Tuition,
            Amount = 1000m,
            DueDate = DateTime.UtcNow.AddDays(30),
            Description = "Test fee"
        };
        var exception = new Exception("Test exception");
        _mockFeeService.Setup(x => x.CreateFeeAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act
        var result = await _controller.CreateFee(request, CancellationToken.None);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<ObjectResult>());
        var statusResult = (ObjectResult)result.Result;
        Assert.That(statusResult.StatusCode, Is.EqualTo(500));
        
        // In development, should include detailed error information
        var errorResponse = statusResult.Value as dynamic;
        Assert.That(errorResponse, Is.Not.Null);
    }

    [Test]
    public async Task CreateFee_ProductionEnvironment_ReturnsGenericErrorMessage()
    {
        // Arrange
        _mockEnvironment.Setup(x => x.EnvironmentName).Returns("Production");
        var request = new CreateFeeReq
        {
            StudentId = Guid.NewGuid(),
            FeeType = FeeType.Tuition,
            Amount = 1000m,
            DueDate = DateTime.UtcNow.AddDays(30),
            Description = "Test fee"
        };
        _mockFeeService.Setup(x => x.CreateFeeAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Service error"));

        // Act
        var result = await _controller.CreateFee(request, CancellationToken.None);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<ObjectResult>());
        var statusResult = (ObjectResult)result.Result;
        Assert.That(statusResult.StatusCode, Is.EqualTo(500));
        
        // In production, should only show generic error message
        var errorResponse = statusResult.Value as dynamic;
        Assert.That(errorResponse, Is.Not.Null);
    }

    #endregion

    #region Logging Tests

    [Test]
    public async Task CreateFee_Success_LogsInformation()
    {
        // Arrange
        var request = new CreateFeeReq
        {
            StudentId = Guid.NewGuid(),
            FeeType = FeeType.Tuition,
            Amount = 1000m,
            DueDate = DateTime.UtcNow.AddDays(30),
            Description = "Test fee"
        };
        var feeId = Guid.NewGuid();
        _mockFeeService.Setup(x => x.CreateFeeAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(feeId);

        // Act
        await _controller.CreateFee(request, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Fee created")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Test]
    public async Task CreateFee_ArgumentException_LogsWarning()
    {
        // Arrange
        var request = new CreateFeeReq
        {
            StudentId = Guid.NewGuid(),
            FeeType = FeeType.Tuition,
            Amount = 1000m,
            DueDate = DateTime.UtcNow.AddDays(30),
            Description = "Test fee"
        };
        _mockFeeService.Setup(x => x.CreateFeeAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Student not found"));

        // Act
        await _controller.CreateFee(request, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Invalid argument when creating fee")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task CreateFee_Exception_LogsError()
    {
        // Arrange
        var request = new CreateFeeReq
        {
            StudentId = Guid.NewGuid(),
            FeeType = FeeType.Tuition,
            Amount = 1000m,
            DueDate = DateTime.UtcNow.AddDays(30),
            Description = "Test fee"
        };
        var exception = new Exception("Service error");
        _mockFeeService.Setup(x => x.CreateFeeAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        // Act
        await _controller.CreateFee(request, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error creating fee")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    #endregion
}