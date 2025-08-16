using EduShield.Api.Data;
using EduShield.Core.Data;
using EduShield.Core.Entities;
using EduShield.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace EduShield.Api.Tests;

[TestFixture]
public class FeeRepoTests
{
    private EduShieldDbContext _context = default!;
    private FeeRepo _repo = default!;
    private Student _testStudent = default!;
    private Student _otherStudent = default!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<EduShieldDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new EduShieldDbContext(options);
        _repo = new FeeRepo(_context);

        // Create test students
        _testStudent = new Student
        {
            Id = Guid.NewGuid(),
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@test.com",
            PhoneNumber = "1234567890",
            DateOfBirth = new DateTime(2000, 1, 1),
            Address = "Test Address",
            EnrollmentDate = DateTime.UtcNow.AddDays(-30),
            Gender = Gender.M,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _otherStudent = new Student
        {
            Id = Guid.NewGuid(),
            FirstName = "Jane",
            LastName = "Smith",
            Email = "jane.smith@test.com",
            PhoneNumber = "0987654321",
            DateOfBirth = new DateTime(2001, 1, 1),
            Address = "Other Address",
            EnrollmentDate = DateTime.UtcNow.AddDays(-20),
            Gender = Gender.F,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Students.Add(_testStudent);
        _context.Students.Add(_otherStudent);
        _context.SaveChanges();
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    #region CRUD Operations Tests

    [Test]
    public async Task CreateAsync_ValidFee_ReturnsCreatedFee()
    {
        // Arrange
        var fee = new Fee
        {
            FeeId = Guid.NewGuid(),
            StudentId = _testStudent.Id,
            FeeType = FeeType.Tuition,
            Amount = 1000.00m,
            PaidAmount = 0m,
            DueDate = DateTime.UtcNow.AddDays(30),
            Description = "Semester Tuition Fee",
            Status = FeeStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        var result = await _repo.CreateAsync(fee);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.FeeId, Is.EqualTo(fee.FeeId));
        Assert.That(result.StudentId, Is.EqualTo(_testStudent.Id));
        Assert.That(result.FeeType, Is.EqualTo(FeeType.Tuition));
        Assert.That(result.Amount, Is.EqualTo(1000.00m));
        Assert.That(result.PaidAmount, Is.EqualTo(0m));
        Assert.That(result.Status, Is.EqualTo(FeeStatus.Pending));
        Assert.That(result.Description, Is.EqualTo("Semester Tuition Fee"));
    }

    [Test]
    public void CreateAsync_NullFee_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsAsync<ArgumentNullException>(() => _repo.CreateAsync(null!));
    }

    [Test]
    public async Task GetByIdAsync_ExistingFee_ReturnsFeeWithNavigationProperties()
    {
        // Arrange
        var fee = new Fee
        {
            FeeId = Guid.NewGuid(),
            StudentId = _testStudent.Id,
            FeeType = FeeType.LabFee,
            Amount = 500.00m,
            PaidAmount = 200.00m,
            DueDate = DateTime.UtcNow.AddDays(15),
            Description = "Lab Equipment Fee",
            Status = FeeStatus.PartiallyPaid,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _repo.CreateAsync(fee);

        // Act
        var result = await _repo.GetByIdAsync(fee.FeeId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.FeeId, Is.EqualTo(fee.FeeId));
        Assert.That(result.Student, Is.Not.Null);
        Assert.That(result.Student!.Id, Is.EqualTo(_testStudent.Id));
        Assert.That(result.Payments, Is.Not.Null);
        Assert.That(result.OutstandingAmount, Is.EqualTo(300.00m));
    }

    [Test]
    public async Task GetByIdAsync_NonExistingFee_ReturnsNull()
    {
        // Act
        var result = await _repo.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetAllAsync_ReturnsAllFeesWithNavigationProperties()
    {
        // Arrange
        var fee1 = new Fee
        {
            FeeId = Guid.NewGuid(),
            StudentId = _testStudent.Id,
            FeeType = FeeType.Tuition,
            Amount = 1000.00m,
            PaidAmount = 0m,
            DueDate = DateTime.UtcNow.AddDays(30),
            Description = "Tuition Fee",
            Status = FeeStatus.Pending
        };

        var fee2 = new Fee
        {
            FeeId = Guid.NewGuid(),
            StudentId = _otherStudent.Id,
            FeeType = FeeType.LibraryFee,
            Amount = 100.00m,
            PaidAmount = 100.00m,
            DueDate = DateTime.UtcNow.AddDays(20),
            Description = "Library Fee",
            Status = FeeStatus.Paid,
            IsPaid = true,
            PaidDate = DateTime.UtcNow
        };

        await _repo.CreateAsync(fee1);
        await _repo.CreateAsync(fee2);

        // Act
        var result = await _repo.GetAllAsync();

        // Assert
        Assert.That(result.Count(), Is.EqualTo(2));
        Assert.That(result.All(f => f.Student != null), Is.True);
        Assert.That(result.All(f => f.Payments != null), Is.True);
    }

    [Test]
    public async Task UpdateAsync_ExistingFee_UpdatesSuccessfully()
    {
        // Arrange
        var fee = new Fee
        {
            FeeId = Guid.NewGuid(),
            StudentId = _testStudent.Id,
            FeeType = FeeType.ActivityFee,
            Amount = 200.00m,
            PaidAmount = 0m,
            DueDate = DateTime.UtcNow.AddDays(10),
            Description = "Activity Fee",
            Status = FeeStatus.Pending
        };
        await _repo.CreateAsync(fee);

        // Modify the fee
        fee.Amount = 250.00m;
        fee.Description = "Updated Activity Fee";
        fee.UpdatedAt = DateTime.UtcNow;

        // Act
        var result = await _repo.UpdateAsync(fee);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Amount, Is.EqualTo(250.00m));
        Assert.That(result.Description, Is.EqualTo("Updated Activity Fee"));
    }

    [Test]
    public void UpdateAsync_NullFee_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsAsync<ArgumentNullException>(() => _repo.UpdateAsync(null!));
    }

    [Test]
    public async Task DeleteAsync_ExistingFee_DeletesSuccessfully()
    {
        // Arrange
        var fee = new Fee
        {
            FeeId = Guid.NewGuid(),
            StudentId = _testStudent.Id,
            FeeType = FeeType.Other,
            Amount = 150.00m,
            PaidAmount = 0m,
            DueDate = DateTime.UtcNow.AddDays(5),
            Description = "Miscellaneous Fee",
            Status = FeeStatus.Pending
        };
        await _repo.CreateAsync(fee);

        // Act
        var result = await _repo.DeleteAsync(fee.FeeId);

        // Assert
        Assert.That(result, Is.True);
        var deletedFee = await _repo.GetByIdAsync(fee.FeeId);
        Assert.That(deletedFee, Is.Null);
    }

    [Test]
    public async Task DeleteAsync_NonExistingFee_ReturnsFalse()
    {
        // Act
        var result = await _repo.DeleteAsync(Guid.NewGuid());

        // Assert
        Assert.That(result, Is.False);
    }

    #endregion

    #region Query Methods Tests

    [Test]
    public async Task GetByStudentIdAsync_ReturnsStudentFeesOrderedByDueDate()
    {
        // Arrange
        var fee1 = new Fee
        {
            FeeId = Guid.NewGuid(),
            StudentId = _testStudent.Id,
            FeeType = FeeType.Tuition,
            Amount = 1000.00m,
            PaidAmount = 0m,
            DueDate = DateTime.UtcNow.AddDays(30), // Later due date
            Description = "Tuition Fee",
            Status = FeeStatus.Pending
        };

        var fee2 = new Fee
        {
            FeeId = Guid.NewGuid(),
            StudentId = _testStudent.Id,
            FeeType = FeeType.LabFee,
            Amount = 200.00m,
            PaidAmount = 0m,
            DueDate = DateTime.UtcNow.AddDays(10), // Earlier due date
            Description = "Lab Fee",
            Status = FeeStatus.Pending
        };

        var otherStudentFee = new Fee
        {
            FeeId = Guid.NewGuid(),
            StudentId = _otherStudent.Id,
            FeeType = FeeType.LibraryFee,
            Amount = 100.00m,
            PaidAmount = 0m,
            DueDate = DateTime.UtcNow.AddDays(20),
            Description = "Library Fee",
            Status = FeeStatus.Pending
        };

        await _repo.CreateAsync(fee1);
        await _repo.CreateAsync(fee2);
        await _repo.CreateAsync(otherStudentFee);

        // Act
        var result = await _repo.GetByStudentIdAsync(_testStudent.Id);

        // Assert
        Assert.That(result.Count(), Is.EqualTo(2));
        Assert.That(result.All(f => f.StudentId == _testStudent.Id), Is.True);
        Assert.That(result.All(f => f.Student != null), Is.True);
        Assert.That(result.All(f => f.Payments != null), Is.True);
        // Should be ordered by due date (earliest first)
        Assert.That(result.First().FeeType, Is.EqualTo(FeeType.LabFee));
        Assert.That(result.Last().FeeType, Is.EqualTo(FeeType.Tuition));
    }

    [Test]
    public async Task GetByFeeTypeAsync_ReturnsFeesByTypeOrderedByDueDate()
    {
        // Arrange
        var tuitionFee1 = new Fee
        {
            FeeId = Guid.NewGuid(),
            StudentId = _testStudent.Id,
            FeeType = FeeType.Tuition,
            Amount = 1000.00m,
            PaidAmount = 0m,
            DueDate = DateTime.UtcNow.AddDays(30),
            Description = "Semester 1 Tuition",
            Status = FeeStatus.Pending
        };

        var tuitionFee2 = new Fee
        {
            FeeId = Guid.NewGuid(),
            StudentId = _otherStudent.Id,
            FeeType = FeeType.Tuition,
            Amount = 1000.00m,
            PaidAmount = 0m,
            DueDate = DateTime.UtcNow.AddDays(15),
            Description = "Semester 1 Tuition",
            Status = FeeStatus.Pending
        };

        var labFee = new Fee
        {
            FeeId = Guid.NewGuid(),
            StudentId = _testStudent.Id,
            FeeType = FeeType.LabFee,
            Amount = 200.00m,
            PaidAmount = 0m,
            DueDate = DateTime.UtcNow.AddDays(20),
            Description = "Lab Fee",
            Status = FeeStatus.Pending
        };

        await _repo.CreateAsync(tuitionFee1);
        await _repo.CreateAsync(tuitionFee2);
        await _repo.CreateAsync(labFee);

        // Act
        var result = await _repo.GetByFeeTypeAsync(FeeType.Tuition);

        // Assert
        Assert.That(result.Count(), Is.EqualTo(2));
        Assert.That(result.All(f => f.FeeType == FeeType.Tuition), Is.True);
        Assert.That(result.All(f => f.Student != null), Is.True);
        Assert.That(result.All(f => f.Payments != null), Is.True);
        // Should be ordered by due date (earliest first)
        Assert.That(result.First().StudentId, Is.EqualTo(_otherStudent.Id));
        Assert.That(result.Last().StudentId, Is.EqualTo(_testStudent.Id));
    }

    [Test]
    public async Task GetOverdueFeesAsync_ReturnsOverdueFeesOrderedByDueDate()
    {
        // Arrange
        var overdueFee1 = new Fee
        {
            FeeId = Guid.NewGuid(),
            StudentId = _testStudent.Id,
            FeeType = FeeType.Tuition,
            Amount = 1000.00m,
            PaidAmount = 500.00m, // Partially paid but still overdue
            DueDate = DateTime.UtcNow.AddDays(-10), // 10 days overdue
            Description = "Overdue Tuition",
            Status = FeeStatus.PartiallyPaid
        };

        var overdueFee2 = new Fee
        {
            FeeId = Guid.NewGuid(),
            StudentId = _otherStudent.Id,
            FeeType = FeeType.LabFee,
            Amount = 200.00m,
            PaidAmount = 0m, // Unpaid and overdue
            DueDate = DateTime.UtcNow.AddDays(-5), // 5 days overdue
            Description = "Overdue Lab Fee",
            Status = FeeStatus.Overdue
        };

        var paidFee = new Fee
        {
            FeeId = Guid.NewGuid(),
            StudentId = _testStudent.Id,
            FeeType = FeeType.LibraryFee,
            Amount = 100.00m,
            PaidAmount = 100.00m, // Fully paid, not overdue
            DueDate = DateTime.UtcNow.AddDays(-3),
            Description = "Paid Library Fee",
            Status = FeeStatus.Paid,
            IsPaid = true
        };

        var futureFee = new Fee
        {
            FeeId = Guid.NewGuid(),
            StudentId = _testStudent.Id,
            FeeType = FeeType.ActivityFee,
            Amount = 150.00m,
            PaidAmount = 0m, // Unpaid but not overdue
            DueDate = DateTime.UtcNow.AddDays(10),
            Description = "Future Activity Fee",
            Status = FeeStatus.Pending
        };

        await _repo.CreateAsync(overdueFee1);
        await _repo.CreateAsync(overdueFee2);
        await _repo.CreateAsync(paidFee);
        await _repo.CreateAsync(futureFee);

        // Act
        var result = await _repo.GetOverdueFeesAsync();

        // Assert
        Assert.That(result.Count(), Is.EqualTo(2));
        Assert.That(result.All(f => f.DueDate < DateTime.UtcNow), Is.True);
        Assert.That(result.All(f => f.PaidAmount < f.Amount), Is.True);
        Assert.That(result.All(f => f.Student != null), Is.True);
        Assert.That(result.All(f => f.Payments != null), Is.True);
        // Should be ordered by due date (earliest first)
        Assert.That(result.First().FeeType, Is.EqualTo(FeeType.Tuition));
        Assert.That(result.Last().FeeType, Is.EqualTo(FeeType.LabFee));
    }

    [Test]
    public async Task GetByStatusAsync_ReturnsFeesByStatusOrderedByDueDate()
    {
        // Arrange
        var pendingFee1 = new Fee
        {
            FeeId = Guid.NewGuid(),
            StudentId = _testStudent.Id,
            FeeType = FeeType.Tuition,
            Amount = 1000.00m,
            PaidAmount = 0m,
            DueDate = DateTime.UtcNow.AddDays(30),
            Description = "Pending Tuition",
            Status = FeeStatus.Pending
        };

        var pendingFee2 = new Fee
        {
            FeeId = Guid.NewGuid(),
            StudentId = _otherStudent.Id,
            FeeType = FeeType.LabFee,
            Amount = 200.00m,
            PaidAmount = 0m,
            DueDate = DateTime.UtcNow.AddDays(15),
            Description = "Pending Lab Fee",
            Status = FeeStatus.Pending
        };

        var paidFee = new Fee
        {
            FeeId = Guid.NewGuid(),
            StudentId = _testStudent.Id,
            FeeType = FeeType.LibraryFee,
            Amount = 100.00m,
            PaidAmount = 100.00m,
            DueDate = DateTime.UtcNow.AddDays(20),
            Description = "Paid Library Fee",
            Status = FeeStatus.Paid,
            IsPaid = true
        };

        await _repo.CreateAsync(pendingFee1);
        await _repo.CreateAsync(pendingFee2);
        await _repo.CreateAsync(paidFee);

        // Act
        var result = await _repo.GetByStatusAsync(FeeStatus.Pending);

        // Assert
        Assert.That(result.Count(), Is.EqualTo(2));
        Assert.That(result.All(f => f.Status == FeeStatus.Pending), Is.True);
        Assert.That(result.All(f => f.Student != null), Is.True);
        Assert.That(result.All(f => f.Payments != null), Is.True);
        // Should be ordered by due date (earliest first)
        Assert.That(result.First().FeeType, Is.EqualTo(FeeType.LabFee));
        Assert.That(result.Last().FeeType, Is.EqualTo(FeeType.Tuition));
    }

    #endregion

    #region Payment Recording Tests

    [Test]
    public async Task AddPaymentAsync_ValidPayment_AddsPaymentAndUpdatesFee()
    {
        // Arrange
        var fee = new Fee
        {
            FeeId = Guid.NewGuid(),
            StudentId = _testStudent.Id,
            FeeType = FeeType.Tuition,
            Amount = 1000.00m,
            PaidAmount = 0m,
            DueDate = DateTime.UtcNow.AddDays(30),
            Description = "Tuition Fee",
            Status = FeeStatus.Pending
        };
        await _repo.CreateAsync(fee);

        var payment = new Payment
        {
            PaymentId = Guid.NewGuid(),
            FeeId = fee.FeeId,
            Amount = 300.00m,
            PaymentDate = DateTime.UtcNow,
            PaymentMethod = "Credit Card",
            TransactionReference = "TXN123456"
        };

        // Act
        var result = await _repo.AddPaymentAsync(payment);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.PaymentId, Is.EqualTo(payment.PaymentId));
        Assert.That(result.Amount, Is.EqualTo(300.00m));

        // Verify fee was updated
        var updatedFee = await _repo.GetByIdAsync(fee.FeeId);
        Assert.That(updatedFee, Is.Not.Null);
        Assert.That(updatedFee!.PaidAmount, Is.EqualTo(300.00m));
        Assert.That(updatedFee.Status, Is.EqualTo(FeeStatus.PartiallyPaid));
        Assert.That(updatedFee.IsPaid, Is.False);
        Assert.That(updatedFee.OutstandingAmount, Is.EqualTo(700.00m));
    }

    [Test]
    public async Task AddPaymentAsync_FullPayment_MarksFeeAsPaid()
    {
        // Arrange
        var fee = new Fee
        {
            FeeId = Guid.NewGuid(),
            StudentId = _testStudent.Id,
            FeeType = FeeType.LabFee,
            Amount = 500.00m,
            PaidAmount = 200.00m, // Already partially paid
            DueDate = DateTime.UtcNow.AddDays(15),
            Description = "Lab Fee",
            Status = FeeStatus.PartiallyPaid
        };
        await _repo.CreateAsync(fee);

        var payment = new Payment
        {
            PaymentId = Guid.NewGuid(),
            FeeId = fee.FeeId,
            Amount = 300.00m, // Remaining amount
            PaymentDate = DateTime.UtcNow,
            PaymentMethod = "Bank Transfer",
            TransactionReference = "TXN789012"
        };

        // Act
        var result = await _repo.AddPaymentAsync(payment);

        // Assert
        Assert.That(result, Is.Not.Null);

        // Verify fee was marked as paid
        var updatedFee = await _repo.GetByIdAsync(fee.FeeId);
        Assert.That(updatedFee, Is.Not.Null);
        Assert.That(updatedFee!.PaidAmount, Is.EqualTo(500.00m));
        Assert.That(updatedFee.Status, Is.EqualTo(FeeStatus.Paid));
        Assert.That(updatedFee.IsPaid, Is.True);
        Assert.That(updatedFee.PaidDate, Is.Not.Null);
        Assert.That(updatedFee.OutstandingAmount, Is.EqualTo(0m));
    }

    [Test]
    public async Task AddPaymentAsync_ExceedsOutstandingAmount_ThrowsInvalidOperationException()
    {
        // Arrange
        var fee = new Fee
        {
            FeeId = Guid.NewGuid(),
            StudentId = _testStudent.Id,
            FeeType = FeeType.LibraryFee,
            Amount = 100.00m,
            PaidAmount = 50.00m,
            DueDate = DateTime.UtcNow.AddDays(10),
            Description = "Library Fee",
            Status = FeeStatus.PartiallyPaid
        };
        await _repo.CreateAsync(fee);

        var payment = new Payment
        {
            PaymentId = Guid.NewGuid(),
            FeeId = fee.FeeId,
            Amount = 60.00m, // Exceeds outstanding amount of 50.00
            PaymentDate = DateTime.UtcNow,
            PaymentMethod = "Cash"
        };

        // Act & Assert
        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => _repo.AddPaymentAsync(payment));
        Assert.That(ex!.Message, Does.Contain("exceeds outstanding amount"));
    }

    [Test]
    public async Task AddPaymentAsync_NonExistentFee_ThrowsInvalidOperationException()
    {
        // Arrange
        var payment = new Payment
        {
            PaymentId = Guid.NewGuid(),
            FeeId = Guid.NewGuid(), // Non-existent fee ID
            Amount = 100.00m,
            PaymentDate = DateTime.UtcNow,
            PaymentMethod = "Credit Card"
        };

        // Act & Assert
        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => _repo.AddPaymentAsync(payment));
        Assert.That(ex!.Message, Does.Contain("not found"));
    }

    [Test]
    public void AddPaymentAsync_NullPayment_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsAsync<ArgumentNullException>(() => _repo.AddPaymentAsync(null!));
    }

    [Test]
    public async Task GetPaymentsByFeeIdAsync_ReturnsPaymentsOrderedByDate()
    {
        // Arrange
        var fee = new Fee
        {
            FeeId = Guid.NewGuid(),
            StudentId = _testStudent.Id,
            FeeType = FeeType.Tuition,
            Amount = 1000.00m,
            PaidAmount = 0m,
            DueDate = DateTime.UtcNow.AddDays(30),
            Description = "Tuition Fee",
            Status = FeeStatus.Pending
        };
        await _repo.CreateAsync(fee);

        var payment1 = new Payment
        {
            PaymentId = Guid.NewGuid(),
            FeeId = fee.FeeId,
            Amount = 300.00m,
            PaymentDate = DateTime.UtcNow.AddDays(-2), // Earlier payment
            PaymentMethod = "Credit Card",
            TransactionReference = "TXN001"
        };

        var payment2 = new Payment
        {
            PaymentId = Guid.NewGuid(),
            FeeId = fee.FeeId,
            Amount = 200.00m,
            PaymentDate = DateTime.UtcNow.AddDays(-1), // Later payment
            PaymentMethod = "Bank Transfer",
            TransactionReference = "TXN002"
        };

        await _repo.AddPaymentAsync(payment1);
        await _repo.AddPaymentAsync(payment2);

        // Act
        var result = await _repo.GetPaymentsByFeeIdAsync(fee.FeeId);

        // Assert
        Assert.That(result.Count(), Is.EqualTo(2));
        Assert.That(result.All(p => p.FeeId == fee.FeeId), Is.True);
        Assert.That(result.All(p => p.Fee != null), Is.True);
        // Should be ordered by payment date (earliest first)
        Assert.That(result.First().TransactionReference, Is.EqualTo("TXN001"));
        Assert.That(result.Last().TransactionReference, Is.EqualTo("TXN002"));
    }

    [Test]
    public async Task GetPaymentsByStudentIdAsync_ReturnsStudentPaymentsOrderedByDate()
    {
        // Arrange
        var fee1 = new Fee
        {
            FeeId = Guid.NewGuid(),
            StudentId = _testStudent.Id,
            FeeType = FeeType.Tuition,
            Amount = 1000.00m,
            PaidAmount = 0m,
            DueDate = DateTime.UtcNow.AddDays(30),
            Description = "Tuition Fee",
            Status = FeeStatus.Pending
        };

        var fee2 = new Fee
        {
            FeeId = Guid.NewGuid(),
            StudentId = _testStudent.Id,
            FeeType = FeeType.LabFee,
            Amount = 200.00m,
            PaidAmount = 0m,
            DueDate = DateTime.UtcNow.AddDays(20),
            Description = "Lab Fee",
            Status = FeeStatus.Pending
        };

        var otherStudentFee = new Fee
        {
            FeeId = Guid.NewGuid(),
            StudentId = _otherStudent.Id,
            FeeType = FeeType.LibraryFee,
            Amount = 100.00m,
            PaidAmount = 0m,
            DueDate = DateTime.UtcNow.AddDays(15),
            Description = "Library Fee",
            Status = FeeStatus.Pending
        };

        await _repo.CreateAsync(fee1);
        await _repo.CreateAsync(fee2);
        await _repo.CreateAsync(otherStudentFee);

        var payment1 = new Payment
        {
            PaymentId = Guid.NewGuid(),
            FeeId = fee1.FeeId,
            Amount = 500.00m,
            PaymentDate = DateTime.UtcNow.AddDays(-3),
            PaymentMethod = "Credit Card"
        };

        var payment2 = new Payment
        {
            PaymentId = Guid.NewGuid(),
            FeeId = fee2.FeeId,
            Amount = 200.00m,
            PaymentDate = DateTime.UtcNow.AddDays(-1),
            PaymentMethod = "Cash"
        };

        var otherStudentPayment = new Payment
        {
            PaymentId = Guid.NewGuid(),
            FeeId = otherStudentFee.FeeId,
            Amount = 100.00m,
            PaymentDate = DateTime.UtcNow.AddDays(-2),
            PaymentMethod = "Bank Transfer"
        };

        await _repo.AddPaymentAsync(payment1);
        await _repo.AddPaymentAsync(payment2);
        await _repo.AddPaymentAsync(otherStudentPayment);

        // Act
        var result = await _repo.GetPaymentsByStudentIdAsync(_testStudent.Id);

        // Assert
        Assert.That(result.Count(), Is.EqualTo(2));
        Assert.That(result.All(p => p.Fee!.StudentId == _testStudent.Id), Is.True);
        Assert.That(result.All(p => p.Fee != null), Is.True);
        Assert.That(result.All(p => p.Fee!.Student != null), Is.True);
        // Should be ordered by payment date (earliest first)
        Assert.That(result.First().Amount, Is.EqualTo(500.00m));
        Assert.That(result.Last().Amount, Is.EqualTo(200.00m));
    }

    #endregion

    #region Specialized Query Tests

    [Test]
    public async Task ExistsAsync_ExistingFee_ReturnsTrue()
    {
        // Arrange
        var fee = new Fee
        {
            FeeId = Guid.NewGuid(),
            StudentId = _testStudent.Id,
            FeeType = FeeType.ActivityFee,
            Amount = 150.00m,
            PaidAmount = 0m,
            DueDate = DateTime.UtcNow.AddDays(20),
            Description = "Activity Fee",
            Status = FeeStatus.Pending
        };
        await _repo.CreateAsync(fee);

        // Act
        var result = await _repo.ExistsAsync(fee.FeeId);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task ExistsAsync_NonExistingFee_ReturnsFalse()
    {
        // Act
        var result = await _repo.ExistsAsync(Guid.NewGuid());

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task GetTotalOutstandingByStudentIdAsync_CalculatesCorrectTotal()
    {
        // Arrange
        var fee1 = new Fee
        {
            FeeId = Guid.NewGuid(),
            StudentId = _testStudent.Id,
            FeeType = FeeType.Tuition,
            Amount = 1000.00m,
            PaidAmount = 300.00m, // Outstanding: 700.00
            DueDate = DateTime.UtcNow.AddDays(30),
            Description = "Tuition Fee",
            Status = FeeStatus.PartiallyPaid
        };

        var fee2 = new Fee
        {
            FeeId = Guid.NewGuid(),
            StudentId = _testStudent.Id,
            FeeType = FeeType.LabFee,
            Amount = 200.00m,
            PaidAmount = 0m, // Outstanding: 200.00
            DueDate = DateTime.UtcNow.AddDays(20),
            Description = "Lab Fee",
            Status = FeeStatus.Pending
        };

        var fee3 = new Fee
        {
            FeeId = Guid.NewGuid(),
            StudentId = _testStudent.Id,
            FeeType = FeeType.LibraryFee,
            Amount = 100.00m,
            PaidAmount = 100.00m, // Outstanding: 0.00
            DueDate = DateTime.UtcNow.AddDays(15),
            Description = "Library Fee",
            Status = FeeStatus.Paid,
            IsPaid = true
        };

        var otherStudentFee = new Fee
        {
            FeeId = Guid.NewGuid(),
            StudentId = _otherStudent.Id,
            FeeType = FeeType.ActivityFee,
            Amount = 150.00m,
            PaidAmount = 0m,
            DueDate = DateTime.UtcNow.AddDays(10),
            Description = "Activity Fee",
            Status = FeeStatus.Pending
        };

        await _repo.CreateAsync(fee1);
        await _repo.CreateAsync(fee2);
        await _repo.CreateAsync(fee3);
        await _repo.CreateAsync(otherStudentFee);

        // Act
        var result = await _repo.GetTotalOutstandingByStudentIdAsync(_testStudent.Id);

        // Assert
        Assert.That(result, Is.EqualTo(900.00m)); // 700.00 + 200.00 + 0.00
    }

    [Test]
    public async Task GetTotalPaidByStudentIdAsync_CalculatesCorrectTotal()
    {
        // Arrange
        var fee1 = new Fee
        {
            FeeId = Guid.NewGuid(),
            StudentId = _testStudent.Id,
            FeeType = FeeType.Tuition,
            Amount = 1000.00m,
            PaidAmount = 300.00m,
            DueDate = DateTime.UtcNow.AddDays(30),
            Description = "Tuition Fee",
            Status = FeeStatus.PartiallyPaid
        };

        var fee2 = new Fee
        {
            FeeId = Guid.NewGuid(),
            StudentId = _testStudent.Id,
            FeeType = FeeType.LabFee,
            Amount = 200.00m,
            PaidAmount = 0m,
            DueDate = DateTime.UtcNow.AddDays(20),
            Description = "Lab Fee",
            Status = FeeStatus.Pending
        };

        var fee3 = new Fee
        {
            FeeId = Guid.NewGuid(),
            StudentId = _testStudent.Id,
            FeeType = FeeType.LibraryFee,
            Amount = 100.00m,
            PaidAmount = 100.00m,
            DueDate = DateTime.UtcNow.AddDays(15),
            Description = "Library Fee",
            Status = FeeStatus.Paid,
            IsPaid = true
        };

        var otherStudentFee = new Fee
        {
            FeeId = Guid.NewGuid(),
            StudentId = _otherStudent.Id,
            FeeType = FeeType.ActivityFee,
            Amount = 150.00m,
            PaidAmount = 75.00m,
            DueDate = DateTime.UtcNow.AddDays(10),
            Description = "Activity Fee",
            Status = FeeStatus.PartiallyPaid
        };

        await _repo.CreateAsync(fee1);
        await _repo.CreateAsync(fee2);
        await _repo.CreateAsync(fee3);
        await _repo.CreateAsync(otherStudentFee);

        // Act
        var result = await _repo.GetTotalPaidByStudentIdAsync(_testStudent.Id);

        // Assert
        Assert.That(result, Is.EqualTo(400.00m)); // 300.00 + 0.00 + 100.00
    }

    #endregion

    #region Entity Relationships and Navigation Properties Tests

    [Test]
    public async Task Fee_NavigationProperties_LoadedCorrectly()
    {
        // Arrange
        var fee = new Fee
        {
            FeeId = Guid.NewGuid(),
            StudentId = _testStudent.Id,
            FeeType = FeeType.Tuition,
            Amount = 1000.00m,
            PaidAmount = 0m,
            DueDate = DateTime.UtcNow.AddDays(30),
            Description = "Tuition Fee",
            Status = FeeStatus.Pending
        };
        await _repo.CreateAsync(fee);

        var payment = new Payment
        {
            PaymentId = Guid.NewGuid(),
            FeeId = fee.FeeId,
            Amount = 250.00m,
            PaymentDate = DateTime.UtcNow,
            PaymentMethod = "Credit Card"
        };
        await _repo.AddPaymentAsync(payment);

        // Act
        var result = await _repo.GetByIdAsync(fee.FeeId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Student, Is.Not.Null);
        Assert.That(result.Student!.Id, Is.EqualTo(_testStudent.Id));
        Assert.That(result.Student.FirstName, Is.EqualTo("John"));
        Assert.That(result.Payments, Is.Not.Null);
        Assert.That(result.Payments.Count, Is.EqualTo(1));
        Assert.That(result.Payments.First().Amount, Is.EqualTo(250.00m));
    }

    [Test]
    public async Task Fee_CalculatedProperties_WorkCorrectly()
    {
        // Arrange
        var fee = new Fee
        {
            FeeId = Guid.NewGuid(),
            StudentId = _testStudent.Id,
            FeeType = FeeType.LabFee,
            Amount = 500.00m,
            PaidAmount = 200.00m,
            DueDate = DateTime.UtcNow.AddDays(-5), // Overdue
            Description = "Lab Fee",
            Status = FeeStatus.PartiallyPaid
        };
        await _repo.CreateAsync(fee);

        // Act
        var result = await _repo.GetByIdAsync(fee.FeeId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.OutstandingAmount, Is.EqualTo(300.00m));
        Assert.That(result.IsOverdue, Is.True);
        Assert.That(result.DaysOverdue, Is.GreaterThan(0));
    }

    #endregion

    #region Error Scenarios and Edge Cases Tests

    [Test]
    public async Task GetByStudentIdAsync_NonExistentStudent_ReturnsEmptyCollection()
    {
        // Act
        var result = await _repo.GetByStudentIdAsync(Guid.NewGuid());

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count(), Is.EqualTo(0));
    }

    [Test]
    public async Task GetByFeeTypeAsync_NoFeesOfType_ReturnsEmptyCollection()
    {
        // Arrange
        var fee = new Fee
        {
            FeeId = Guid.NewGuid(),
            StudentId = _testStudent.Id,
            FeeType = FeeType.Tuition,
            Amount = 1000.00m,
            PaidAmount = 0m,
            DueDate = DateTime.UtcNow.AddDays(30),
            Description = "Tuition Fee",
            Status = FeeStatus.Pending
        };
        await _repo.CreateAsync(fee);

        // Act
        var result = await _repo.GetByFeeTypeAsync(FeeType.LibraryFee);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count(), Is.EqualTo(0));
    }

    [Test]
    public async Task GetOverdueFeesAsync_NoOverdueFees_ReturnsEmptyCollection()
    {
        // Arrange
        var fee = new Fee
        {
            FeeId = Guid.NewGuid(),
            StudentId = _testStudent.Id,
            FeeType = FeeType.Tuition,
            Amount = 1000.00m,
            PaidAmount = 0m,
            DueDate = DateTime.UtcNow.AddDays(30), // Future due date
            Description = "Tuition Fee",
            Status = FeeStatus.Pending
        };
        await _repo.CreateAsync(fee);

        // Act
        var result = await _repo.GetOverdueFeesAsync();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count(), Is.EqualTo(0));
    }

    [Test]
    public async Task GetPaymentsByFeeIdAsync_NoPayments_ReturnsEmptyCollection()
    {
        // Arrange
        var fee = new Fee
        {
            FeeId = Guid.NewGuid(),
            StudentId = _testStudent.Id,
            FeeType = FeeType.ActivityFee,
            Amount = 150.00m,
            PaidAmount = 0m,
            DueDate = DateTime.UtcNow.AddDays(20),
            Description = "Activity Fee",
            Status = FeeStatus.Pending
        };
        await _repo.CreateAsync(fee);

        // Act
        var result = await _repo.GetPaymentsByFeeIdAsync(fee.FeeId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count(), Is.EqualTo(0));
    }

    [Test]
    public async Task GetTotalOutstandingByStudentIdAsync_NoFees_ReturnsZero()
    {
        // Act
        var result = await _repo.GetTotalOutstandingByStudentIdAsync(Guid.NewGuid());

        // Assert
        Assert.That(result, Is.EqualTo(0m));
    }

    [Test]
    public async Task GetTotalPaidByStudentIdAsync_NoFees_ReturnsZero()
    {
        // Act
        var result = await _repo.GetTotalPaidByStudentIdAsync(Guid.NewGuid());

        // Assert
        Assert.That(result, Is.EqualTo(0m));
    }

    [Test]
    public async Task AddPaymentAsync_ZeroAmount_AllowsZeroPayment()
    {
        // Arrange
        var fee = new Fee
        {
            FeeId = Guid.NewGuid(),
            StudentId = _testStudent.Id,
            FeeType = FeeType.LibraryFee,
            Amount = 100.00m,
            PaidAmount = 0m,
            DueDate = DateTime.UtcNow.AddDays(10),
            Description = "Library Fee",
            Status = FeeStatus.Pending
        };
        await _repo.CreateAsync(fee);

        var payment = new Payment
        {
            PaymentId = Guid.NewGuid(),
            FeeId = fee.FeeId,
            Amount = 0m, // Zero amount
            PaymentDate = DateTime.UtcNow,
            PaymentMethod = "Cash"
        };

        // Act
        var result = await _repo.AddPaymentAsync(payment);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Amount, Is.EqualTo(0m));
        
        // Verify fee remains unchanged
        var updatedFee = await _repo.GetByIdAsync(fee.FeeId);
        Assert.That(updatedFee, Is.Not.Null);
        Assert.That(updatedFee!.PaidAmount, Is.EqualTo(0m));
        Assert.That(updatedFee.Status, Is.EqualTo(FeeStatus.Pending));
    }

    #endregion
}