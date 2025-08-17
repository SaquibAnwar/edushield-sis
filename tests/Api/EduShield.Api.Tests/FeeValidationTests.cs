using FluentValidation.TestHelper;
using EduShield.Core.Dtos;
using EduShield.Core.Entities;
using EduShield.Core.Enums;
using EduShield.Core.Validators;
using NUnit.Framework;

namespace EduShield.Api.Tests;

public class FeeValidationTests
{
    [Test]
    public void CreateFeeReqValidator_ValidRequest_ShouldNotHaveValidationError()
    {
        // Arrange
        var validator = new CreateFeeReqValidator();
        var request = new CreateFeeReq
        {
            StudentId = Guid.NewGuid(),
            FeeType = FeeType.Tuition,
            Amount = 1000.50m,
            DueDate = DateTime.Today.AddDays(30),
            Description = "Tuition fee for semester"
        };

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void CreateFeeReqValidator_EmptyStudentId_ShouldHaveValidationError()
    {
        // Arrange
        var validator = new CreateFeeReqValidator();
        var request = new CreateFeeReq
        {
            StudentId = Guid.Empty,
            FeeType = FeeType.Tuition,
            Amount = 1000.50m,
            DueDate = DateTime.Today.AddDays(30)
        };

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.StudentId);
    }

    [Test]
    public void CreateFeeReqValidator_NegativeAmount_ShouldHaveValidationError()
    {
        // Arrange
        var validator = new CreateFeeReqValidator();
        var request = new CreateFeeReq
        {
            StudentId = Guid.NewGuid(),
            FeeType = FeeType.Tuition,
            Amount = -100m,
            DueDate = DateTime.Today.AddDays(30)
        };

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Test]
    public void CreateFeeReqValidator_PastDueDate_ShouldHaveValidationError()
    {
        // Arrange
        var validator = new CreateFeeReqValidator();
        var request = new CreateFeeReq
        {
            StudentId = Guid.NewGuid(),
            FeeType = FeeType.Tuition,
            Amount = 1000m,
            DueDate = DateTime.Today.AddDays(-1)
        };

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DueDate);
    }

    [Test]
    public void PaymentReqValidator_ValidRequest_ShouldNotHaveValidationError()
    {
        // Arrange
        var validator = new PaymentReqValidator();
        var request = new PaymentReq
        {
            Amount = 500.25m,
            PaymentDate = DateTime.Today,
            PaymentMethod = "Credit Card",
            TransactionReference = "TXN123456"
        };

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void PaymentReqValidator_FuturePaymentDate_ShouldHaveValidationError()
    {
        // Arrange
        var validator = new PaymentReqValidator();
        var request = new PaymentReq
        {
            Amount = 500m,
            PaymentDate = DateTime.Today.AddDays(1),
            PaymentMethod = "Cash"
        };

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PaymentDate);
    }

    [Test]
    public void PaymentReqValidator_InvalidPaymentMethod_ShouldHaveValidationError()
    {
        // Arrange
        var validator = new PaymentReqValidator();
        var request = new PaymentReq
        {
            Amount = 500m,
            PaymentDate = DateTime.Today,
            PaymentMethod = "Bitcoin" // Invalid payment method
        };

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PaymentMethod);
    }

    [Test]
    public void PaymentBusinessValidator_PaymentExceedsOutstanding_ShouldHaveValidationError()
    {
        // Arrange
        var validator = new PaymentBusinessValidator();
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
        var paymentRequest = new PaymentReq
        {
            Amount = 500m, // Exceeds outstanding amount of 400m
            PaymentDate = DateTime.Today,
            PaymentMethod = "Cash"
        };
        var context = new PaymentValidationContext
        {
            PaymentRequest = paymentRequest,
            Fee = fee
        };

        // Act
        var result = validator.TestValidate(context);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PaymentRequest.Amount);
    }

    [Test]
    public void UpdateFeeBusinessValidator_ModifyPaidFee_ShouldHaveValidationError()
    {
        // Arrange
        var validator = new UpdateFeeBusinessValidator();
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
        var updateRequest = new UpdateFeeReq
        {
            FeeType = FeeType.Tuition,
            Amount = 1200m,
            DueDate = DateTime.Today.AddDays(30),
            Description = "Updated fee"
        };
        var context = new UpdateFeeValidationContext
        {
            UpdateRequest = updateRequest,
            Fee = fee
        };

        // Act
        var result = validator.TestValidate(context);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Fee.Status);
    }

}