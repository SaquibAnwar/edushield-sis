using AutoMapper;
using FluentValidation;
using FluentValidation.Results;
using EduShield.Api.Services;
using EduShield.Core.Dtos;
using EduShield.Core.Entities;
using EduShield.Core.Enums;
using EduShield.Core.Exceptions;
using EduShield.Core.Interfaces;
using EduShield.Core.Mapping;
using EduShield.Core.Validators;
using Moq;

namespace EduShield.Api.Tests;

[TestFixture]
public class FeeServiceTests
{
    private Mock<IFeeRepo> _mockFeeRepo = default!;
    private Mock<IStudentRepo> _mockStudentRepo = default!;
    private Mock<IValidator<CreateFeeReq>> _mockCreateFeeValidator = default!;
    private Mock<IValidator<UpdateFeeReq>> _mockUpdateFeeValidator = default!;
    private Mock<IValidator<PaymentReq>> _mockPaymentValidator = default!;
    private Mock<IValidator<PaymentValidationContext>> _mockPaymentBusinessValidator = default!;
    private Mock<IValidator<UpdateFeeValidationContext>> _mockUpdateFeeBusinessValidator = default!;
    private IMapper _mapper = default!;
    private FeeService _service = default!;

    [SetUp]
    public void SetUp()
    {
        _mockFeeRepo = new Mock<IFeeRepo>();
        _mockStudentRepo = new Mock<IStudentRepo>();
        _mockCreateFeeValidator = new Mock<IValidator<CreateFeeReq>>();
        _mockUpdateFeeValidator = new Mock<IValidator<UpdateFeeReq>>();
        _mockPaymentValidator = new Mock<IValidator<PaymentReq>>();
        _mockPaymentBusinessValidator = new Mock<IValidator<PaymentValidationContext>>();
        _mockUpdateFeeBusinessValidator = new Mock<IValidator<UpdateFeeValidationContext>>();

        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<FeeMappingProfile>();
            cfg.AddProfile<PaymentMappingProfile>();
        });
        _mapper = config.CreateMapper();

        _service = new FeeService(
            _mockFeeRepo.Object,
            _mockStudentRepo.Object,
            _mapper,
            _mockCreateFeeValidator.Object,
            _mockUpdateFeeValidator.Object,
            _mockPaymentValidator.Object,
            _mockPaymentBusinessValidator.Object,
            _mockUpdateFeeBusinessValidator.Object);
    }

    #region CreateFeeAsync Tests

    [Test]
    public async Task CreateFeeAsync_ValidRequest_ReturnsNewFeeId()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var request = new CreateFeeReq
        {
            StudentId = studentId,
            FeeType = FeeType.Tuition,
            Amount = 1000m,
            DueDate = DateTime.Today.AddDays(30),
            Description = "Tuition fee"
        };

        var student = CreateTestStudent(studentId);
        var createdFee = CreateTestFee(Guid.NewGuid(), studentId);

        _mockCreateFeeValidator.Setup(x => x.ValidateAsync(request))
            .ReturnsAsync(new ValidationResult());
        _mockStudentRepo.Setup(x => x.GetByIdAsync(It.IsAny<CancellationToken>())).ReturnsAsync(student);
        _mockFeeRepo.Setup(x => x.CreateAsync(It.IsAny<Fee>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdFee);

        // Act
        var result = await _service.CreateFeeAsync(request);

        // Assert
        Assert.That(result, Is.Not.EqualTo(Guid.Empty));
        _mockFeeRepo.Verify(x => x.CreateAsync(It.Is<Fee>(f => 
            f.StudentId == studentId &&
            f.FeeType == FeeType.Tuition &&
            f.Amount == 1000m &&
            f.PaidAmount == 0 &&
            f.Status == FeeStatus.Pending &&
            f.IsPaid == false
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task CreateFeeAsync_ValidationFails_ThrowsValidationException()
    {
        // Arrange
        var request = new CreateFeeReq
        {
            StudentId = Guid.Empty,
            FeeType = FeeType.Tuition,
            Amount = -100m,
            DueDate = DateTime.Today.AddDays(-1)
        };

        var validationFailures = new List<ValidationFailure>
        {
            new("StudentId", "Student ID is required"),
            new("Amount", "Amount must be positive")
        };
        var validationResult = new ValidationResult(validationFailures);

        _mockCreateFeeValidator.Setup(x => x.ValidateAsync(It.IsAny<CancellationToken>())).ReturnsAsync(validationResult);

        // Act & Assert
        var ex = Assert.ThrowsAsync<ValidationException>(async () => await _service.CreateFeeAsync(request));
        Assert.That(ex.Errors.Count(), Is.EqualTo(2));
    }

    [Test]
    public async Task CreateFeeAsync_StudentNotFound_ThrowsArgumentException()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var request = new CreateFeeReq
        {
            StudentId = studentId,
            FeeType = FeeType.Tuition,
            Amount = 1000m,
            DueDate = DateTime.Today.AddDays(30)
        };

        _mockCreateFeeValidator.Setup(x => x.ValidateAsync(request))
            .ReturnsAsync(new ValidationResult());
        _mockStudentRepo.Setup(x => x.GetByIdAsync(studentId, CancellationToken.None))
            .ReturnsAsync((Student?)null);

        // Act & Assert
        var ex = Assert.ThrowsAsync<StudentNotFoundException>(async () => await _service.CreateFeeAsync(request));
        Assert.That(ex.Message, Does.Contain($"Student with ID '{studentId}' was not found"));
    }

    #endregion

    #region GetFeeByIdAsync Tests

    [Test]
    public async Task GetFeeByIdAsync_ExistingFee_ReturnsFeeDto()
    {
        // Arrange
        var feeId = Guid.NewGuid();
        var fee = CreateTestFee(feeId, Guid.NewGuid());
        var payments = new List<Payment>
        {
            CreateTestPayment(Guid.NewGuid(), feeId, 500m)
        };

        _mockFeeRepo.Setup(x => x.GetByIdAsync(It.IsAny<CancellationToken>())).ReturnsAsync(fee);
        _mockFeeRepo.Setup(x => x.GetPaymentsByFeeIdAsync(It.IsAny<CancellationToken>())).ReturnsAsync(payments);

        // Act
        var result = await _service.GetFeeByIdAsync(feeId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.FeeId, Is.EqualTo(feeId));
        Assert.That(result.Payments.Count, Is.EqualTo(1));
        Assert.That(result.Payments[0].Amount, Is.EqualTo(500m));
    }

    [Test]
    public async Task GetFeeByIdAsync_NonExistingFee_ReturnsNull()
    {
        // Arrange
        var feeId = Guid.NewGuid();
        _mockFeeRepo.Setup(x => x.GetByIdAsync(feeId))
            .ReturnsAsync((Fee?)null);

        // Act
        var result = await _service.GetFeeByIdAsync(feeId);

        // Assert
        Assert.That(result, Is.Null);
    }

    #endregion

    #region UpdateFeeAsync Tests

    [Test]
    public async Task UpdateFeeAsync_ValidRequest_ReturnsTrue()
    {
        // Arrange
        var feeId = Guid.NewGuid();
        var existingFee = CreateTestFee(feeId, Guid.NewGuid());
        var request = new UpdateFeeReq
        {
            FeeType = FeeType.LabFee,
            Amount = 1200m,
            DueDate = DateTime.Today.AddDays(45),
            Description = "Updated lab fee"
        };

        _mockUpdateFeeValidator.Setup(x => x.ValidateAsync(request))
            .ReturnsAsync(new ValidationResult());
        _mockFeeRepo.Setup(x => x.GetByIdAsync(It.IsAny<CancellationToken>())).ReturnsAsync(existingFee);
        _mockUpdateFeeBusinessValidator.Setup(x => x.ValidateAsync(It.IsAny<UpdateFeeValidationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        _mockFeeRepo.Setup(x => x.UpdateAsync(It.IsAny<Fee>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingFee);

        // Act
        var result = await _service.UpdateFeeAsync(feeId, request);

        // Assert
        Assert.That(result, Is.True);
        _mockFeeRepo.Verify(x => x.UpdateAsync(It.Is<Fee>(f => 
            f.FeeType == FeeType.LabFee &&
            f.Amount == 1200m &&
            f.Description == "Updated lab fee"
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task UpdateFeeAsync_NonExistingFee_ReturnsFalse()
    {
        // Arrange
        var feeId = Guid.NewGuid();
        var request = new UpdateFeeReq
        {
            FeeType = FeeType.LabFee,
            Amount = 1200m,
            DueDate = DateTime.Today.AddDays(45)
        };

        _mockUpdateFeeValidator.Setup(x => x.ValidateAsync(request))
            .ReturnsAsync(new ValidationResult());
        _mockFeeRepo.Setup(x => x.GetByIdAsync(feeId))
            .ReturnsAsync((Fee?)null);

        // Act
        var result = await _service.UpdateFeeAsync(feeId, request);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task UpdateFeeAsync_BusinessValidationFails_ThrowsValidationException()
    {
        // Arrange
        var feeId = Guid.NewGuid();
        var existingFee = CreateTestFee(feeId, Guid.NewGuid());
        existingFee.Status = FeeStatus.Paid;
        var request = new UpdateFeeReq
        {
            FeeType = FeeType.LabFee,
            Amount = 1200m,
            DueDate = DateTime.Today.AddDays(45)
        };

        var validationFailures = new List<ValidationFailure>
        {
            new("Fee.Status", "Cannot modify a paid fee")
        };
        var validationResult = new ValidationResult(validationFailures);

        _mockUpdateFeeValidator.Setup(x => x.ValidateAsync(request))
            .ReturnsAsync(new ValidationResult());
        _mockFeeRepo.Setup(x => x.GetByIdAsync(It.IsAny<CancellationToken>())).ReturnsAsync(existingFee);
        _mockUpdateFeeBusinessValidator.Setup(x => x.ValidateAsync(It.IsAny<UpdateFeeValidationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act & Assert
        var ex = Assert.ThrowsAsync<ValidationException>(async () => await _service.UpdateFeeAsync(feeId, request));
        Assert.That(ex.Errors.Count(), Is.EqualTo(1));
    }

    #endregion

    #region DeleteFeeAsync Tests

    [Test]
    public async Task DeleteFeeAsync_FeeWithoutPayments_ReturnsTrue()
    {
        // Arrange
        var feeId = Guid.NewGuid();
        var existingFee = CreateTestFee(feeId, Guid.NewGuid());
        var emptyPayments = new List<Payment>();

        _mockFeeRepo.Setup(x => x.GetByIdAsync(It.IsAny<CancellationToken>())).ReturnsAsync(existingFee);
        _mockFeeRepo.Setup(x => x.GetPaymentsByFeeIdAsync(It.IsAny<CancellationToken>())).ReturnsAsync(emptyPayments);
        _mockFeeRepo.Setup(x => x.DeleteAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act
        var result = await _service.DeleteFeeAsync(feeId);

        // Assert
        Assert.That(result, Is.True);
        _mockFeeRepo.Verify(x => x.DeleteAsync(feeId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task DeleteFeeAsync_FeeWithPayments_ReturnsTrue()
    {
        // Arrange
        var feeId = Guid.NewGuid();
        var existingFee = CreateTestFee(feeId, Guid.NewGuid());

        _mockFeeRepo.Setup(x => x.GetByIdAsync(It.IsAny<CancellationToken>())).ReturnsAsync(existingFee);
        _mockFeeRepo.Setup(x => x.DeleteAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act
        var result = await _service.DeleteFeeAsync(feeId);

        // Assert
        Assert.That(result, Is.True);
        _mockFeeRepo.Verify(x => x.DeleteAsync(feeId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task DeleteFeeAsync_NonExistingFee_ReturnsFalse()
    {
        // Arrange
        var feeId = Guid.NewGuid();
        _mockFeeRepo.Setup(x => x.GetByIdAsync(feeId))
            .ReturnsAsync((Fee?)null);

        // Act
        var result = await _service.DeleteFeeAsync(feeId);

        // Assert
        Assert.That(result, Is.False);
    }

    #endregion

    #region RecordPaymentAsync Tests

    [Test]
    public async Task RecordPaymentAsync_ValidPayment_ReturnsPaymentDto()
    {
        // Arrange
        var feeId = Guid.NewGuid();
        var fee = CreateTestFee(feeId, Guid.NewGuid());
        fee.Amount = 1000m;
        fee.PaidAmount = 0m;
        fee.Status = FeeStatus.Pending;

        var paymentRequest = new PaymentReq
        {
            Amount = 500m,
            PaymentDate = DateTime.Today,
            PaymentMethod = "Credit Card",
            TransactionReference = "TXN123"
        };

        var createdPayment = CreateTestPayment(Guid.NewGuid(), feeId, 500m);

        _mockPaymentValidator.Setup(x => x.ValidateAsync(paymentRequest))
            .ReturnsAsync(new ValidationResult());
        _mockFeeRepo.Setup(x => x.GetByIdAsync(It.IsAny<CancellationToken>())).ReturnsAsync(fee);
        _mockPaymentBusinessValidator.Setup(x => x.ValidateAsync(It.IsAny<PaymentValidationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        _mockFeeRepo.Setup(x => x.AddPaymentAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdPayment);
        _mockFeeRepo.Setup(x => x.UpdateAsync(It.IsAny<Fee>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(fee);

        // Act
        var result = await _service.RecordPaymentAsync(feeId, paymentRequest);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Amount, Is.EqualTo(500m));
        _mockFeeRepo.Verify(x => x.UpdateAsync(It.Is<Fee>(f => 
            f.PaidAmount == 500m &&
            f.Status == FeeStatus.PartiallyPaid
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task RecordPaymentAsync_FullPayment_MarksFeeAsPaid()
    {
        // Arrange
        var feeId = Guid.NewGuid();
        var fee = CreateTestFee(feeId, Guid.NewGuid());
        fee.Amount = 1000m;
        fee.PaidAmount = 0m;
        fee.Status = FeeStatus.Pending;

        var paymentRequest = new PaymentReq
        {
            Amount = 1000m,
            PaymentDate = DateTime.Today,
            PaymentMethod = "Credit Card"
        };

        var createdPayment = CreateTestPayment(Guid.NewGuid(), feeId, 1000m);

        _mockPaymentValidator.Setup(x => x.ValidateAsync(paymentRequest))
            .ReturnsAsync(new ValidationResult());
        _mockFeeRepo.Setup(x => x.GetByIdAsync(It.IsAny<CancellationToken>())).ReturnsAsync(fee);
        _mockPaymentBusinessValidator.Setup(x => x.ValidateAsync(It.IsAny<PaymentValidationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        _mockFeeRepo.Setup(x => x.AddPaymentAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdPayment);
        _mockFeeRepo.Setup(x => x.UpdateAsync(It.IsAny<Fee>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(fee);

        // Act
        var result = await _service.RecordPaymentAsync(feeId, paymentRequest);

        // Assert
        Assert.That(result, Is.Not.Null);
        _mockFeeRepo.Verify(x => x.UpdateAsync(It.Is<Fee>(f => 
            f.PaidAmount == 1000m &&
            f.Status == FeeStatus.Paid &&
            f.IsPaid == true &&
            f.PaidDate != null
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task RecordPaymentAsync_FeeNotFound_ThrowsArgumentException()
    {
        // Arrange
        var feeId = Guid.NewGuid();
        var paymentRequest = new PaymentReq
        {
            Amount = 500m,
            PaymentDate = DateTime.Today,
            PaymentMethod = "Credit Card"
        };

        _mockPaymentValidator.Setup(x => x.ValidateAsync(paymentRequest))
            .ReturnsAsync(new ValidationResult());
        _mockFeeRepo.Setup(x => x.GetByIdAsync(feeId))
            .ReturnsAsync((Fee?)null);

        // Act & Assert
        var ex = Assert.ThrowsAsync<FeeNotFoundException>(async () => await _service.RecordPaymentAsync(feeId, paymentRequest));
        Assert.That(ex.Message, Does.Contain($"Fee with ID '{feeId}' was not found"));
    }

    [Test]
    public async Task RecordPaymentAsync_PaymentExceedsOutstanding_ThrowsValidationException()
    {
        // Arrange
        var feeId = Guid.NewGuid();
        var fee = CreateTestFee(feeId, Guid.NewGuid());
        fee.Amount = 1000m;
        fee.PaidAmount = 600m;

        var paymentRequest = new PaymentReq
        {
            Amount = 500m, // Exceeds outstanding amount of 400m
            PaymentDate = DateTime.Today,
            PaymentMethod = "Credit Card"
        };

        var validationFailures = new List<ValidationFailure>
        {
            new("PaymentRequest.Amount", "Payment amount exceeds outstanding balance")
        };
        var validationResult = new ValidationResult(validationFailures);

        _mockPaymentValidator.Setup(x => x.ValidateAsync(paymentRequest))
            .ReturnsAsync(new ValidationResult());
        _mockFeeRepo.Setup(x => x.GetByIdAsync(It.IsAny<CancellationToken>())).ReturnsAsync(fee);
        _mockPaymentBusinessValidator.Setup(x => x.ValidateAsync(It.IsAny<PaymentValidationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act & Assert
        var ex = Assert.ThrowsAsync<ValidationException>(async () => await _service.RecordPaymentAsync(feeId, paymentRequest));
        Assert.That(ex.Errors.Count(), Is.EqualTo(1));
    }

    #endregion

    #region GetStudentFeesSummaryAsync Tests

    [Test]
    public async Task GetStudentFeesSummaryAsync_ValidStudent_ReturnsSummary()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var student = CreateTestStudent(studentId);
        var fees = new List<Fee>
        {
            CreateTestFee(Guid.NewGuid(), studentId, 1000m, 1000m, FeeStatus.Paid),
            CreateTestFee(Guid.NewGuid(), studentId, 500m, 200m, FeeStatus.PartiallyPaid),
            CreateTestFee(Guid.NewGuid(), studentId, 300m, 0m, FeeStatus.Overdue, DateTime.Today.AddDays(-10))
        };
        var payments = new List<Payment>
        {
            CreateTestPayment(Guid.NewGuid(), fees[0].FeeId, 1000m),
            CreateTestPayment(Guid.NewGuid(), fees[1].FeeId, 200m)
        };

        _mockStudentRepo.Setup(x => x.GetByIdAsync(It.IsAny<CancellationToken>())).ReturnsAsync(student);
        _mockFeeRepo.Setup(x => x.GetByStudentIdAsync(It.IsAny<CancellationToken>())).ReturnsAsync(fees);
        _mockFeeRepo.Setup(x => x.GetPaymentsByStudentIdAsync(It.IsAny<CancellationToken>())).ReturnsAsync(payments);

        // Act
        var result = await _service.GetStudentFeesSummaryAsync(studentId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StudentId, Is.EqualTo(studentId));
        Assert.That(result.TotalFees, Is.EqualTo(1800m));
        Assert.That(result.TotalPaid, Is.EqualTo(1200m));
        Assert.That(result.TotalOutstanding, Is.EqualTo(600m));
        Assert.That(result.TotalOverdue, Is.EqualTo(300m));
        Assert.That(result.TotalFeeCount, Is.EqualTo(3));
        Assert.That(result.PaidFeeCount, Is.EqualTo(1));
        Assert.That(result.OverdueFeeCount, Is.EqualTo(1));
        Assert.That(result.Fees.Count, Is.EqualTo(3));
        Assert.That(result.RecentPayments.Count, Is.EqualTo(2));
    }

    [Test]
    public async Task GetStudentFeesSummaryAsync_StudentNotFound_ThrowsArgumentException()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        _mockStudentRepo.Setup(x => x.GetByIdAsync(studentId, CancellationToken.None))
            .ReturnsAsync((Student?)null);

        // Act & Assert
        var ex = Assert.ThrowsAsync<StudentNotFoundException>(async () => await _service.GetStudentFeesSummaryAsync(studentId));
        Assert.That(ex.Message, Does.Contain($"Student with ID '{studentId}' was not found"));
    }

    #endregion

    #region Query Tests

    [Test]
    public async Task GetAllFeesAsync_ReturnsAllFees()
    {
        // Arrange
        var fees = new List<Fee>
        {
            CreateTestFee(Guid.NewGuid(), Guid.NewGuid()),
            CreateTestFee(Guid.NewGuid(), Guid.NewGuid())
        };

        _mockFeeRepo.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(fees);

        // Act
        var result = await _service.GetAllFeesAsync();

        // Assert
        Assert.That(result.Count(), Is.EqualTo(2));
    }

    [Test]
    public async Task GetFeesByStudentIdAsync_ReturnsStudentFees()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var fees = new List<Fee>
        {
            CreateTestFee(Guid.NewGuid(), studentId)
        };

        _mockFeeRepo.Setup(x => x.GetByStudentIdAsync(It.IsAny<CancellationToken>())).ReturnsAsync(fees);

        // Act
        var result = await _service.GetFeesByStudentIdAsync(studentId);

        // Assert
        Assert.That(result.Count(), Is.EqualTo(1));
        Assert.That(result.First().StudentId, Is.EqualTo(studentId));
    }

    [Test]
    public async Task GetFeesByTypeAsync_ReturnsFeesByType()
    {
        // Arrange
        var fees = new List<Fee>
        {
            CreateTestFee(Guid.NewGuid(), Guid.NewGuid(), feeType: FeeType.Tuition)
        };

        _mockFeeRepo.Setup(x => x.GetByFeeTypeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(fees);

        // Act
        var result = await _service.GetFeesByTypeAsync(FeeType.Tuition);

        // Assert
        Assert.That(result.Count(), Is.EqualTo(1));
        Assert.That(result.First().FeeType, Is.EqualTo(FeeType.Tuition));
    }

    [Test]
    public async Task GetOverdueFeesAsync_ReturnsOverdueFees()
    {
        // Arrange
        var fees = new List<Fee>
        {
            CreateTestFee(Guid.NewGuid(), Guid.NewGuid(), status: FeeStatus.Overdue, dueDate: DateTime.Today.AddDays(-5))
        };

        _mockFeeRepo.Setup(x => x.GetOverdueFeesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(fees);

        // Act
        var result = await _service.GetOverdueFeesAsync();

        // Assert
        Assert.That(result.Count(), Is.EqualTo(1));
        Assert.That(result.First().Status, Is.EqualTo(FeeStatus.Overdue));
    }

    #endregion

    #region Business Logic Tests

    [Test]
    public async Task MarkFeeAsPaidAsync_ExistingFee_ReturnsTrue()
    {
        // Arrange
        var feeId = Guid.NewGuid();
        var fee = CreateTestFee(feeId, Guid.NewGuid());
        fee.Amount = 1000m;
        fee.PaidAmount = 500m;

        _mockFeeRepo.Setup(x => x.GetByIdAsync(It.IsAny<CancellationToken>())).ReturnsAsync(fee);
        _mockFeeRepo.Setup(x => x.UpdateAsync(It.IsAny<Fee>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(fee);

        // Act
        var result = await _service.MarkFeeAsPaidAsync(feeId);

        // Assert
        Assert.That(result, Is.True);
        _mockFeeRepo.Verify(x => x.UpdateAsync(It.Is<Fee>(f => 
            f.PaidAmount == f.Amount &&
            f.Status == FeeStatus.Paid &&
            f.IsPaid == true &&
            f.PaidDate != null
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task UpdateFeeStatusesAsync_UpdatesAllFeeStatuses()
    {
        // Arrange
        var fee1 = CreateTestFee(Guid.NewGuid(), Guid.NewGuid(), 1000m, 1000m, FeeStatus.PartiallyPaid); // Should become Paid
        var fee2 = CreateTestFee(Guid.NewGuid(), Guid.NewGuid(), 500m, 0m, FeeStatus.Pending, DateTime.Today.AddDays(-5)); // Should become Overdue
        
        var fees = new List<Fee> { fee1, fee2 };

        _mockFeeRepo.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(fees);
        _mockFeeRepo.Setup(x => x.UpdateAsync(It.IsAny<Fee>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Fee f, CancellationToken ct) => f);

        // Act
        await _service.UpdateFeeStatusesAsync();

        // Assert
        // Both fees should have status changes, so both should be updated
        _mockFeeRepo.Verify(x => x.UpdateAsync(It.Is<Fee>(f => f.FeeId == fee1.FeeId), It.IsAny<CancellationToken>()), Times.Once);
        _mockFeeRepo.Verify(x => x.UpdateAsync(It.Is<Fee>(f => f.FeeId == fee2.FeeId), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Fee Status Calculation Tests

    [Test]
    public async Task RecordPaymentAsync_PartialPayment_UpdatesStatusCorrectly()
    {
        // Arrange
        var feeId = Guid.NewGuid();
        var fee = CreateTestFee(feeId, Guid.NewGuid());
        fee.Amount = 1000m;
        fee.PaidAmount = 0m;
        fee.Status = FeeStatus.Pending;
        fee.DueDate = DateTime.Today.AddDays(10); // Not overdue

        var paymentRequest = new PaymentReq
        {
            Amount = 300m,
            PaymentDate = DateTime.Today,
            PaymentMethod = "Credit Card"
        };

        var createdPayment = CreateTestPayment(Guid.NewGuid(), feeId, 300m);

        _mockPaymentValidator.Setup(x => x.ValidateAsync(paymentRequest))
            .ReturnsAsync(new ValidationResult());
        _mockFeeRepo.Setup(x => x.GetByIdAsync(It.IsAny<CancellationToken>())).ReturnsAsync(fee);
        _mockPaymentBusinessValidator.Setup(x => x.ValidateAsync(It.IsAny<PaymentValidationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        _mockFeeRepo.Setup(x => x.AddPaymentAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdPayment);
        _mockFeeRepo.Setup(x => x.UpdateAsync(It.IsAny<Fee>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(fee);

        // Act
        await _service.RecordPaymentAsync(feeId, paymentRequest);

        // Assert
        _mockFeeRepo.Verify(x => x.UpdateAsync(It.Is<Fee>(f => 
            f.PaidAmount == 300m &&
            f.Status == FeeStatus.PartiallyPaid &&
            f.IsPaid == false &&
            f.PaidDate == null
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task RecordPaymentAsync_PartialPaymentOnOverdueFee_KeepsOverdueStatus()
    {
        // Arrange
        var feeId = Guid.NewGuid();
        var fee = CreateTestFee(feeId, Guid.NewGuid());
        fee.Amount = 1000m;
        fee.PaidAmount = 0m;
        fee.Status = FeeStatus.Overdue;
        fee.DueDate = DateTime.Today.AddDays(-5); // Overdue

        var paymentRequest = new PaymentReq
        {
            Amount = 300m,
            PaymentDate = DateTime.Today,
            PaymentMethod = "Credit Card"
        };

        var createdPayment = CreateTestPayment(Guid.NewGuid(), feeId, 300m);

        _mockPaymentValidator.Setup(x => x.ValidateAsync(paymentRequest))
            .ReturnsAsync(new ValidationResult());
        _mockFeeRepo.Setup(x => x.GetByIdAsync(It.IsAny<CancellationToken>())).ReturnsAsync(fee);
        _mockPaymentBusinessValidator.Setup(x => x.ValidateAsync(It.IsAny<PaymentValidationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        _mockFeeRepo.Setup(x => x.AddPaymentAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdPayment);
        _mockFeeRepo.Setup(x => x.UpdateAsync(It.IsAny<Fee>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(fee);

        // Act
        await _service.RecordPaymentAsync(feeId, paymentRequest);

        // Assert
        _mockFeeRepo.Verify(x => x.UpdateAsync(It.Is<Fee>(f => 
            f.PaidAmount == 300m &&
            f.Status == FeeStatus.Overdue &&
            f.IsPaid == false
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task UpdateFeeStatusesAsync_NoStatusChanges_DoesNotUpdateFees()
    {
        // Arrange
        var fee1 = CreateTestFee(Guid.NewGuid(), Guid.NewGuid(), 1000m, 1000m, FeeStatus.Paid);
        var fee2 = CreateTestFee(Guid.NewGuid(), Guid.NewGuid(), 500m, 200m, FeeStatus.PartiallyPaid, DateTime.Today.AddDays(10));
        
        var fees = new List<Fee> { fee1, fee2 };

        _mockFeeRepo.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(fees);

        // Act
        await _service.UpdateFeeStatusesAsync();

        // Assert
        // No fees should be updated since their statuses are already correct
        _mockFeeRepo.Verify(x => x.UpdateAsync(It.IsAny<Fee>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Validation Error Tests

    [Test]
    public async Task CreateFeeAsync_InvalidAmount_ThrowsValidationException()
    {
        // Arrange
        var request = new CreateFeeReq
        {
            StudentId = Guid.NewGuid(),
            FeeType = FeeType.Tuition,
            Amount = -100m, // Invalid negative amount
            DueDate = DateTime.Today.AddDays(30)
        };

        var validationFailures = new List<ValidationFailure>
        {
            new("Amount", "Amount must be positive")
        };
        var validationResult = new ValidationResult(validationFailures);

        _mockCreateFeeValidator.Setup(x => x.ValidateAsync(It.IsAny<CancellationToken>())).ReturnsAsync(validationResult);

        // Act & Assert
        var ex = Assert.ThrowsAsync<ValidationException>(async () => await _service.CreateFeeAsync(request));
        Assert.That(ex.Errors.Count(), Is.EqualTo(1));
        Assert.That(ex.Errors.First().ErrorMessage, Is.EqualTo("Amount must be positive"));
    }

    [Test]
    public async Task CreateFeeAsync_PastDueDate_ThrowsValidationException()
    {
        // Arrange
        var request = new CreateFeeReq
        {
            StudentId = Guid.NewGuid(),
            FeeType = FeeType.Tuition,
            Amount = 1000m,
            DueDate = DateTime.Today.AddDays(-1) // Past due date
        };

        var validationFailures = new List<ValidationFailure>
        {
            new("DueDate", "Due date cannot be in the past")
        };
        var validationResult = new ValidationResult(validationFailures);

        _mockCreateFeeValidator.Setup(x => x.ValidateAsync(It.IsAny<CancellationToken>())).ReturnsAsync(validationResult);

        // Act & Assert
        var ex = Assert.ThrowsAsync<ValidationException>(async () => await _service.CreateFeeAsync(request));
        Assert.That(ex.Errors.Count(), Is.EqualTo(1));
        Assert.That(ex.Errors.First().ErrorMessage, Is.EqualTo("Due date cannot be in the past"));
    }

    [Test]
    public async Task UpdateFeeAsync_ValidationFails_ThrowsValidationException()
    {
        // Arrange
        var feeId = Guid.NewGuid();
        var request = new UpdateFeeReq
        {
            FeeType = FeeType.Tuition,
            Amount = 0m, // Invalid zero amount
            DueDate = DateTime.Today.AddDays(30)
        };

        var validationFailures = new List<ValidationFailure>
        {
            new("Amount", "Amount must be greater than zero")
        };
        var validationResult = new ValidationResult(validationFailures);

        _mockUpdateFeeValidator.Setup(x => x.ValidateAsync(It.IsAny<CancellationToken>())).ReturnsAsync(validationResult);

        // Act & Assert
        var ex = Assert.ThrowsAsync<ValidationException>(async () => await _service.UpdateFeeAsync(feeId, request));
        Assert.That(ex.Errors.Count(), Is.EqualTo(1));
        Assert.That(ex.Errors.First().ErrorMessage, Is.EqualTo("Amount must be greater than zero"));
    }

    [Test]
    public async Task RecordPaymentAsync_ValidationFails_ThrowsValidationException()
    {
        // Arrange
        var feeId = Guid.NewGuid();
        var paymentRequest = new PaymentReq
        {
            Amount = -100m, // Invalid negative amount
            PaymentDate = DateTime.Today,
            PaymentMethod = ""
        };

        var validationFailures = new List<ValidationFailure>
        {
            new("Amount", "Payment amount must be positive"),
            new("PaymentMethod", "Payment method is required")
        };
        var validationResult = new ValidationResult(validationFailures);

        _mockPaymentValidator.Setup(x => x.ValidateAsync(It.IsAny<CancellationToken>())).ReturnsAsync(validationResult);

        // Act & Assert
        var ex = Assert.ThrowsAsync<ValidationException>(async () => await _service.RecordPaymentAsync(feeId, paymentRequest));
        Assert.That(ex.Errors.Count(), Is.EqualTo(2));
    }

    #endregion

    #region Edge Cases and Error Scenarios

    [Test]
    public async Task GetStudentFeesSummaryAsync_StudentWithNoFees_ReturnsEmptySummary()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var student = CreateTestStudent(studentId);
        var emptyFees = new List<Fee>();
        var emptyPayments = new List<Payment>();

        _mockStudentRepo.Setup(x => x.GetByIdAsync(It.IsAny<CancellationToken>())).ReturnsAsync(student);
        _mockFeeRepo.Setup(x => x.GetByStudentIdAsync(It.IsAny<CancellationToken>())).ReturnsAsync(emptyFees);
        _mockFeeRepo.Setup(x => x.GetPaymentsByStudentIdAsync(It.IsAny<CancellationToken>())).ReturnsAsync(emptyPayments);

        // Act
        var result = await _service.GetStudentFeesSummaryAsync(studentId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StudentId, Is.EqualTo(studentId));
        Assert.That(result.TotalFees, Is.EqualTo(0m));
        Assert.That(result.TotalPaid, Is.EqualTo(0m));
        Assert.That(result.TotalOutstanding, Is.EqualTo(0m));
        Assert.That(result.TotalOverdue, Is.EqualTo(0m));
        Assert.That(result.TotalFeeCount, Is.EqualTo(0));
        Assert.That(result.PaidFeeCount, Is.EqualTo(0));
        Assert.That(result.OverdueFeeCount, Is.EqualTo(0));
        Assert.That(result.PendingFeeCount, Is.EqualTo(0));
        Assert.That(result.Fees.Count, Is.EqualTo(0));
        Assert.That(result.RecentPayments.Count, Is.EqualTo(0));
    }

    [Test]
    public async Task GetStudentFeesSummaryAsync_StudentWithManyPayments_LimitsRecentPayments()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var student = CreateTestStudent(studentId);
        var fees = new List<Fee>
        {
            CreateTestFee(Guid.NewGuid(), studentId, 1000m, 1000m, FeeStatus.Paid)
        };
        
        // Create 15 payments (should only return 10 most recent)
        var payments = new List<Payment>();
        for (int i = 0; i < 15; i++)
        {
            payments.Add(new Payment
            {
                PaymentId = Guid.NewGuid(),
                FeeId = fees[0].FeeId,
                Amount = 100m,
                PaymentDate = DateTime.Today.AddDays(-i), // Different dates for ordering
                PaymentMethod = "Credit Card",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        _mockStudentRepo.Setup(x => x.GetByIdAsync(It.IsAny<CancellationToken>())).ReturnsAsync(student);
        _mockFeeRepo.Setup(x => x.GetByStudentIdAsync(It.IsAny<CancellationToken>())).ReturnsAsync(fees);
        _mockFeeRepo.Setup(x => x.GetPaymentsByStudentIdAsync(It.IsAny<CancellationToken>())).ReturnsAsync(payments);

        // Act
        var result = await _service.GetStudentFeesSummaryAsync(studentId);

        // Assert
        Assert.That(result.RecentPayments.Count, Is.EqualTo(10));
        // Verify payments are ordered by date descending (most recent first)
        for (int i = 0; i < result.RecentPayments.Count - 1; i++)
        {
            Assert.That(result.RecentPayments[i].PaymentDate, Is.GreaterThanOrEqualTo(result.RecentPayments[i + 1].PaymentDate));
        }
    }

    [Test]
    public async Task MarkFeeAsPaidAsync_NonExistingFee_ReturnsFalse()
    {
        // Arrange
        var feeId = Guid.NewGuid();
        _mockFeeRepo.Setup(x => x.GetByIdAsync(feeId))
            .ReturnsAsync((Fee?)null);

        // Act
        var result = await _service.MarkFeeAsPaidAsync(feeId);

        // Assert
        Assert.That(result, Is.False);
        _mockFeeRepo.Verify(x => x.UpdateAsync(It.IsAny<Fee>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task GetFeesByStatusAsync_ReturnsCorrectFees()
    {
        // Arrange
        var fees = new List<Fee>
        {
            CreateTestFee(Guid.NewGuid(), Guid.NewGuid(), status: FeeStatus.Paid),
            CreateTestFee(Guid.NewGuid(), Guid.NewGuid(), status: FeeStatus.Pending)
        };

        _mockFeeRepo.Setup(x => x.GetByStatusAsync(FeeStatus.Paid))
            .ReturnsAsync(fees.Where(f => f.Status == FeeStatus.Paid));

        // Act
        var result = await _service.GetFeesByStatusAsync(FeeStatus.Paid);

        // Assert
        Assert.That(result.Count(), Is.EqualTo(1));
        Assert.That(result.First().Status, Is.EqualTo(FeeStatus.Paid));
    }

    [Test]
    public async Task GetPaymentsByFeeIdAsync_ReturnsCorrectPayments()
    {
        // Arrange
        var feeId = Guid.NewGuid();
        var payments = new List<Payment>
        {
            CreateTestPayment(Guid.NewGuid(), feeId, 500m),
            CreateTestPayment(Guid.NewGuid(), feeId, 300m)
        };

        _mockFeeRepo.Setup(x => x.ExistsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _mockFeeRepo.Setup(x => x.GetPaymentsByFeeIdAsync(It.IsAny<CancellationToken>())).ReturnsAsync(payments);

        // Act
        var result = await _service.GetPaymentsByFeeIdAsync(feeId);

        // Assert
        Assert.That(result.Count(), Is.EqualTo(2));
        Assert.That(result.Sum(p => p.Amount), Is.EqualTo(800m));
    }

    [Test]
    public async Task GetPaymentsByStudentIdAsync_ReturnsCorrectPayments()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var payments = new List<Payment>
        {
            CreateTestPayment(Guid.NewGuid(), Guid.NewGuid(), 500m),
            CreateTestPayment(Guid.NewGuid(), Guid.NewGuid(), 300m)
        };

        _mockFeeRepo.Setup(x => x.GetPaymentsByStudentIdAsync(It.IsAny<CancellationToken>())).ReturnsAsync(payments);

        // Act
        var result = await _service.GetPaymentsByStudentIdAsync(studentId);

        // Assert
        Assert.That(result.Count(), Is.EqualTo(2));
        Assert.That(result.Sum(p => p.Amount), Is.EqualTo(800m));
    }

    #endregion

    #region Concurrency and Transaction Tests

    [Test]
    public async Task RecordPaymentAsync_ConcurrentPayments_HandlesCorrectly()
    {
        // Arrange
        var feeId = Guid.NewGuid();
        var fee = CreateTestFee(feeId, Guid.NewGuid());
        fee.Amount = 1000m;
        fee.PaidAmount = 800m; // Already has some payments
        fee.Status = FeeStatus.PartiallyPaid;

        var paymentRequest = new PaymentReq
        {
            Amount = 200m, // Exactly the remaining amount
            PaymentDate = DateTime.Today,
            PaymentMethod = "Credit Card"
        };

        var createdPayment = CreateTestPayment(Guid.NewGuid(), feeId, 200m);

        _mockPaymentValidator.Setup(x => x.ValidateAsync(paymentRequest))
            .ReturnsAsync(new ValidationResult());
        _mockFeeRepo.Setup(x => x.GetByIdAsync(It.IsAny<CancellationToken>())).ReturnsAsync(fee);
        _mockPaymentBusinessValidator.Setup(x => x.ValidateAsync(It.IsAny<PaymentValidationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        _mockFeeRepo.Setup(x => x.AddPaymentAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdPayment);
        _mockFeeRepo.Setup(x => x.UpdateAsync(It.IsAny<Fee>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(fee);

        // Act
        var result = await _service.RecordPaymentAsync(feeId, paymentRequest);

        // Assert
        Assert.That(result, Is.Not.Null);
        _mockFeeRepo.Verify(x => x.UpdateAsync(It.Is<Fee>(f => 
            f.PaidAmount == 1000m &&
            f.Status == FeeStatus.Paid &&
            f.IsPaid == true &&
            f.PaidDate != null
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Business Rule Validation Tests

    [Test]
    public async Task CreateFeeAsync_SetsCorrectInitialValues()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var request = new CreateFeeReq
        {
            StudentId = studentId,
            FeeType = FeeType.LibraryFee,
            Amount = 250m,
            DueDate = DateTime.Today.AddDays(60),
            Description = "Library late fee"
        };

        var student = CreateTestStudent(studentId);
        var createdFee = CreateTestFee(Guid.NewGuid(), studentId);

        _mockCreateFeeValidator.Setup(x => x.ValidateAsync(request))
            .ReturnsAsync(new ValidationResult());
        _mockStudentRepo.Setup(x => x.GetByIdAsync(It.IsAny<CancellationToken>())).ReturnsAsync(student);
        _mockFeeRepo.Setup(x => x.CreateAsync(It.IsAny<Fee>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdFee);

        // Act
        var result = await _service.CreateFeeAsync(request);

        // Assert
        _mockFeeRepo.Verify(x => x.CreateAsync(It.Is<Fee>(f => 
            f.StudentId == studentId &&
            f.FeeType == FeeType.LibraryFee &&
            f.Amount == 250m &&
            f.PaidAmount == 0m &&
            f.Status == FeeStatus.Pending &&
            f.IsPaid == false &&
            f.PaidDate == null &&
            f.Description == "Library late fee" &&
            f.CreatedAt != default &&
            f.UpdatedAt != default
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task UpdateFeeAsync_RecalculatesStatusCorrectly()
    {
        // Arrange
        var feeId = Guid.NewGuid();
        var existingFee = CreateTestFee(feeId, Guid.NewGuid());
        existingFee.Amount = 1000m;
        existingFee.PaidAmount = 500m;
        existingFee.Status = FeeStatus.PartiallyPaid;

        var request = new UpdateFeeReq
        {
            FeeType = FeeType.ActivityFee,
            Amount = 500m, // Reducing amount to match paid amount
            DueDate = DateTime.Today.AddDays(30),
            Description = "Updated activity fee"
        };

        _mockUpdateFeeValidator.Setup(x => x.ValidateAsync(request))
            .ReturnsAsync(new ValidationResult());
        _mockFeeRepo.Setup(x => x.GetByIdAsync(It.IsAny<CancellationToken>())).ReturnsAsync(existingFee);
        _mockUpdateFeeBusinessValidator.Setup(x => x.ValidateAsync(It.IsAny<UpdateFeeValidationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
        _mockFeeRepo.Setup(x => x.UpdateAsync(It.IsAny<Fee>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingFee);

        // Act
        var result = await _service.UpdateFeeAsync(feeId, request);

        // Assert
        Assert.That(result, Is.True);
        _mockFeeRepo.Verify(x => x.UpdateAsync(It.Is<Fee>(f => 
            f.Amount == 500m &&
            f.Status == FeeStatus.Paid && // Should be marked as paid since PaidAmount >= Amount
            f.IsPaid == true
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Helper Methods

    private static Student CreateTestStudent(Guid studentId)
    {
        return new Student
        {
            Id = studentId,
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@test.com",
            PhoneNumber = "1234567890",
            DateOfBirth = new DateTime(2000, 1, 1),
            Address = "Test Address",
            EnrollmentDate = DateTime.Today.AddDays(-30),
            Gender = Gender.M,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private static Fee CreateTestFee(Guid feeId, Guid studentId, decimal amount = 1000m, decimal paidAmount = 0m, 
        FeeStatus status = FeeStatus.Pending, DateTime? dueDate = null, FeeType feeType = FeeType.Tuition)
    {
        var actualDueDate = dueDate ?? DateTime.Today.AddDays(30);
        var fee = new Fee
            {
                FeeId = Guid.NewGuid(),
                StudentId = Guid.NewGuid(),
                Amount = 100.00m,
                FeeType = FeeType.Tuition,
                Description = "Test Fee",
                DueDate = DateTime.UtcNow.AddDays(30),
                CreatedAt = DateTime.UtcNow
            };

        return fee;
    }

    private static Payment CreateTestPayment(Guid paymentId, Guid feeId, decimal amount)
    {
        return new Payment
        {
            PaymentId = paymentId,
            FeeId = feeId,
            Amount = amount,
            PaymentDate = DateTime.Today,
            PaymentMethod = "Credit Card",
            TransactionReference = "TXN123",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    #endregion

}