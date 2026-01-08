using Moq;
using ReMindHealth.Application.Interfaces;
using ReMindHealth.Application.Interfaces.IRepositories;
using ReMindHealth.Application.Interfaces.IServices;
using ReMindHealth.Application.Services.Implementation.Domain;
using ReMindHealth.Domain.Models;

namespace ReMindHealth.Tests;

public class AppointmentServiceTests
{
	private readonly Mock<IUnitOfWork> _mockUnitOfWork;
	private readonly Mock<IUserService> _mockUserService;
	private readonly AppointmentService _appointmentService;

	public AppointmentServiceTests()
	{
		_mockUnitOfWork = new Mock<IUnitOfWork>();
		_mockUserService = new Mock<IUserService>();

		// Default Repository Mock
		_mockUnitOfWork
			.Setup(x => x.AppointmentRepository)
			.Returns(Mock.Of<IAppointmentRepository>());

		_appointmentService = new AppointmentService(
			_mockUnitOfWork.Object,
			_mockUserService.Object
		);
	}

	#region GetAppointmentAsync Tests

	[Fact]
	public async Task GetAppointmentAsync_ShouldReturnAppointment_WhenExists()
	{
		// Arrange
		var appointmentId = Guid.NewGuid();
		var expectedAppointment = new ExtractedAppointment
		{
			AppointmentId = appointmentId,
			Title = "Doctor Visit"
		};

		var mockRepo = new Mock<IAppointmentRepository>();
		mockRepo
			.Setup(x => x.GetByIdAsync(appointmentId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(expectedAppointment);

		_mockUnitOfWork.Setup(x => x.AppointmentRepository).Returns(mockRepo.Object);

		// Act
		var result = await _appointmentService.GetAppointmentAsync(appointmentId);

		// Assert
		Assert.NotNull(result);
		Assert.Equal(appointmentId, result.AppointmentId);
		Assert.Equal("Doctor Visit", result.Title);
	}

	[Fact]
	public async Task GetAppointmentAsync_ShouldReturnNull_WhenNotExists()
	{
		// Arrange
		var appointmentId = Guid.NewGuid();

		var mockRepo = new Mock<IAppointmentRepository>();
		mockRepo
			.Setup(x => x.GetByIdAsync(appointmentId, It.IsAny<CancellationToken>()))
			.ReturnsAsync((ExtractedAppointment?)null);

		_mockUnitOfWork.Setup(x => x.AppointmentRepository).Returns(mockRepo.Object);

		// Act
		var result = await _appointmentService.GetAppointmentAsync(appointmentId);

		// Assert
		Assert.Null(result);
	}

	#endregion

	#region GetUserAppointmentsAsync Tests

	[Fact]
	public async Task GetUserAppointmentsAsync_ShouldReturnAppointmentsForCurrentUser()
	{
		// Arrange
		var userId = "user123";
		var appointments = new List<ExtractedAppointment>
		{
			new ExtractedAppointment { Title = "A1", UserId = userId },
			new ExtractedAppointment { Title = "A2", UserId = userId }
		};

		_mockUserService
			.Setup(x => x.GetCurrentUserIdAsync())
			.ReturnsAsync(userId);

		var mockRepo = new Mock<IAppointmentRepository>();
		mockRepo
			.Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(appointments);

		_mockUnitOfWork.Setup(x => x.AppointmentRepository).Returns(mockRepo.Object);

		// Act
		var result = await _appointmentService.GetUserAppointmentsAsync();

		// Assert
		Assert.Equal(2, result.Count);
		Assert.All(result, a => Assert.Equal(userId, a.UserId));
	}

	[Fact]
	public async Task GetUserAppointmentsAsync_ShouldReturnEmptyList_WhenNoneExist()
	{
		// Arrange
		var userId = "user123";

		_mockUserService
			.Setup(x => x.GetCurrentUserIdAsync())
			.ReturnsAsync(userId);

		var mockRepo = new Mock<IAppointmentRepository>();
		mockRepo
			.Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(new List<ExtractedAppointment>());

		_mockUnitOfWork.Setup(x => x.AppointmentRepository).Returns(mockRepo.Object);

		// Act
		var result = await _appointmentService.GetUserAppointmentsAsync();

		// Assert
		Assert.Empty(result);
	}

	#endregion

	#region GetUpcomingAppointmentsAsync Tests

	[Fact]
	public async Task GetUpcomingAppointmentsAsync_ShouldReturnUpcomingAppointments()
	{
		// Arrange
		var userId = "user123";
		var days = 14;

		var appointments = new List<ExtractedAppointment>
		{
			new ExtractedAppointment { Title = "Upcoming 1" },
			new ExtractedAppointment { Title = "Upcoming 2" }
		};

		_mockUserService
			.Setup(x => x.GetCurrentUserIdAsync())
			.ReturnsAsync(userId);

		var mockRepo = new Mock<IAppointmentRepository>();
		mockRepo
			.Setup(x => x.GetUpcomingByUserIdAsync(userId, days, It.IsAny<CancellationToken>()))
			.ReturnsAsync(appointments);

		_mockUnitOfWork.Setup(x => x.AppointmentRepository).Returns(mockRepo.Object);

		// Act
		var result = await _appointmentService.GetUpcomingAppointmentsAsync(days);

		// Assert
		Assert.Equal(2, result.Count);
		mockRepo.Verify(
			x => x.GetUpcomingByUserIdAsync(userId, days, It.IsAny<CancellationToken>()),
			Times.Once
		);
	}

	#endregion

	#region CreateAppointmentAsync Tests

	[Fact]
	public async Task CreateAppointmentAsync_ShouldAddAppointment_AndSave()
	{
		// Arrange
		var appointment = new ExtractedAppointment
		{
			Title = "New Appointment"
		};

		var mockRepo = new Mock<IAppointmentRepository>();
		mockRepo
		.Setup(x => x.AddAsync(It.IsAny<ExtractedAppointment>(), It.IsAny<CancellationToken>()))
		.ReturnsAsync((ExtractedAppointment a, CancellationToken _) => a);


		_mockUnitOfWork.Setup(x => x.AppointmentRepository).Returns(mockRepo.Object);
		_mockUnitOfWork.Setup(x => x.SaveAsync(It.IsAny<CancellationToken>()))
					   .Returns(Task.CompletedTask);

		// Act
		var result = await _appointmentService.CreateAppointmentAsync(appointment);

		// Assert
		Assert.Equal(appointment, result);
		mockRepo.Verify(x => x.AddAsync(appointment, It.IsAny<CancellationToken>()), Times.Once);
		_mockUnitOfWork.Verify(x => x.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
	}

	#endregion

	#region UpdateAppointmentAsync Tests

	[Fact]
	public async Task UpdateAppointmentAsync_ShouldUpdateAppointment_AndSave()
	{
		// Arrange
		var appointment = new ExtractedAppointment
		{
			AppointmentId = Guid.NewGuid(),
			Title = "Updated"
		};

		var mockRepo = new Mock<IAppointmentRepository>();
		mockRepo
			.Setup(x => x.UpdateAsync(appointment, It.IsAny<CancellationToken>()))
			.Returns(Task.CompletedTask);

		_mockUnitOfWork.Setup(x => x.AppointmentRepository).Returns(mockRepo.Object);
		_mockUnitOfWork.Setup(x => x.SaveAsync(It.IsAny<CancellationToken>()))
					   .Returns(Task.CompletedTask);

		// Act
		await _appointmentService.UpdateAppointmentAsync(appointment);

		// Assert
		mockRepo.Verify(x => x.UpdateAsync(appointment, It.IsAny<CancellationToken>()), Times.Once);
		_mockUnitOfWork.Verify(x => x.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
	}

	#endregion

	#region DeleteAppointmentAsync Tests

	[Fact]
	public async Task DeleteAppointmentAsync_ShouldDeleteAppointment_AndSave()
	{
		// Arrange
		var appointmentId = Guid.NewGuid();

		var mockRepo = new Mock<IAppointmentRepository>();
		mockRepo
			.Setup(x => x.DeleteAsync(appointmentId, It.IsAny<CancellationToken>()))
			.Returns(Task.CompletedTask);

		_mockUnitOfWork.Setup(x => x.AppointmentRepository).Returns(mockRepo.Object);
		_mockUnitOfWork.Setup(x => x.SaveAsync(It.IsAny<CancellationToken>()))
					   .Returns(Task.CompletedTask);

		// Act
		await _appointmentService.DeleteAppointmentAsync(appointmentId);

		// Assert
		mockRepo.Verify(x => x.DeleteAsync(appointmentId, It.IsAny<CancellationToken>()), Times.Once);
		_mockUnitOfWork.Verify(x => x.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
	}

	#endregion
}

