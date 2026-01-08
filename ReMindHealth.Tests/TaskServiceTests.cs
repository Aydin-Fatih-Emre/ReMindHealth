using Moq;
using ReMindHealth.Application.Interfaces;
using ReMindHealth.Application.Interfaces.IRepositories;
using ReMindHealth.Application.Interfaces.IServices;
using ReMindHealth.Application.Services.Implementation.Domain;
using ReMindHealth.Domain.Models;

namespace ReMindHealth.Tests;

public class TaskServiceTests
{
	private readonly Mock<IUnitOfWork> _mockUnitOfWork;
	private readonly Mock<IUserService> _mockUserService;
	private readonly TaskService _taskService;

	public TaskServiceTests()
	{
		_mockUnitOfWork = new Mock<IUnitOfWork>();
		_mockUserService = new Mock<IUserService>();

		_mockUnitOfWork
			.Setup(x => x.TaskRepository)
			.Returns(Mock.Of<ITaskRepository>());

		_taskService = new TaskService(
			_mockUnitOfWork.Object,
			_mockUserService.Object
		);
	}

	// =============================
	// GetTaskAsync
	// =============================

	[Fact]
	public async Task GetTaskAsync_ShouldReturnTask_WhenExists()
	{
		// Arrange
		var taskId = Guid.NewGuid();
		var task = new ExtractedTask
		{
			TaskId = taskId,
			Title = "Test Task"
		};

		var repo = new Mock<ITaskRepository>();
		repo.Setup(x => x.GetByIdAsync(taskId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(task);

		_mockUnitOfWork.Setup(x => x.TaskRepository).Returns(repo.Object);

		// Act
		var result = await _taskService.GetTaskAsync(taskId);

		// Assert
		Assert.NotNull(result);
		Assert.Equal(taskId, result!.TaskId);
		Assert.Equal("Test Task", result.Title);
	}

	[Fact]
	public async Task GetTaskAsync_ShouldReturnNull_WhenNotFound()
	{
		// Arrange
		var taskId = Guid.NewGuid();

		var repo = new Mock<ITaskRepository>();
		repo.Setup(x => x.GetByIdAsync(taskId, It.IsAny<CancellationToken>()))
			.ReturnsAsync((ExtractedTask?)null);

		_mockUnitOfWork.Setup(x => x.TaskRepository).Returns(repo.Object);

		// Act
		var result = await _taskService.GetTaskAsync(taskId);

		// Assert
		Assert.Null(result);
	}

	// =============================
	// GetUserTasksAsync
	// =============================

	[Fact]
	public async Task GetUserTasksAsync_ShouldReturnTasksForCurrentUser()
	{
		// Arrange
		var userId = "user123";
		var tasks = new List<ExtractedTask>
		{
			new ExtractedTask { Title = "Task 1", UserId = userId },
			new ExtractedTask { Title = "Task 2", UserId = userId }
		};

		_mockUserService
			.Setup(x => x.GetCurrentUserIdAsync())
			.ReturnsAsync(userId);

		var repo = new Mock<ITaskRepository>();
		repo.Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(tasks);

		_mockUnitOfWork.Setup(x => x.TaskRepository).Returns(repo.Object);

		// Act
		var result = await _taskService.GetUserTasksAsync();

		// Assert
		Assert.Equal(2, result.Count);
		Assert.All(result, t => Assert.Equal(userId, t.UserId));
	}

	// =============================
	// GetPendingTasksAsync
	// =============================

	[Fact]
	public async Task GetPendingTasksAsync_ShouldReturnPendingTasks()
	{
		// Arrange
		var userId = "user123";
		var tasks = new List<ExtractedTask>
		{
			new ExtractedTask { Title = "Pending 1", IsCompleted = false },
			new ExtractedTask { Title = "Pending 2", IsCompleted = false }
		};

		_mockUserService
			.Setup(x => x.GetCurrentUserIdAsync())
			.ReturnsAsync(userId);

		var repo = new Mock<ITaskRepository>();
		repo.Setup(x => x.GetPendingByUserIdAsync(userId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(tasks);

		_mockUnitOfWork.Setup(x => x.TaskRepository).Returns(repo.Object);

		// Act
		var result = await _taskService.GetPendingTasksAsync();

		// Assert
		Assert.Equal(2, result.Count);
		Assert.All(result, t => Assert.False(t.IsCompleted));
	}

	// =============================
	// CreateTaskAsync
	// =============================

	[Fact]
	public async Task CreateTaskAsync_ShouldAddTask_AndSave()
	{
		// Arrange
		var task = new ExtractedTask
		{
			Title = "New Task"
		};

		var repo = new Mock<ITaskRepository>();
		repo.Setup(x => x.AddAsync(It.IsAny<ExtractedTask>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync((ExtractedTask t, CancellationToken _) => t);

		_mockUnitOfWork.Setup(x => x.TaskRepository).Returns(repo.Object);
		_mockUnitOfWork.Setup(x => x.SaveAsync(It.IsAny<CancellationToken>()))
			.Returns(Task.CompletedTask);

		// Act
		var result = await _taskService.CreateTaskAsync(task);

		// Assert
		Assert.Equal(task, result);
		repo.Verify(x => x.AddAsync(task, It.IsAny<CancellationToken>()), Times.Once);
		_mockUnitOfWork.Verify(x => x.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
	}

	// =============================
	// UpdateTaskAsync
	// =============================

	[Fact]
	public async Task UpdateTaskAsync_ShouldUpdateTask_AndSave()
	{
		// Arrange
		var task = new ExtractedTask
		{
			TaskId = Guid.NewGuid(),
			Title = "Updated Task"
		};

		var repo = new Mock<ITaskRepository>();
		repo.Setup(x => x.UpdateAsync(task, It.IsAny<CancellationToken>()))
			.Returns(Task.CompletedTask);

		_mockUnitOfWork.Setup(x => x.TaskRepository).Returns(repo.Object);
		_mockUnitOfWork.Setup(x => x.SaveAsync(It.IsAny<CancellationToken>()))
			.Returns(Task.CompletedTask);

		// Act
		await _taskService.UpdateTaskAsync(task);

		// Assert
		repo.Verify(x => x.UpdateAsync(task, It.IsAny<CancellationToken>()), Times.Once);
		_mockUnitOfWork.Verify(x => x.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
	}

	// =============================
	// DeleteTaskAsync
	// =============================

	[Fact]
	public async Task DeleteTaskAsync_ShouldDeleteTask_AndSave()
	{
		// Arrange
		var taskId = Guid.NewGuid();

		var repo = new Mock<ITaskRepository>();
		repo.Setup(x => x.DeleteAsync(taskId, It.IsAny<CancellationToken>()))
			.Returns(Task.CompletedTask);

		_mockUnitOfWork.Setup(x => x.TaskRepository).Returns(repo.Object);
		_mockUnitOfWork.Setup(x => x.SaveAsync(It.IsAny<CancellationToken>()))
			.Returns(Task.CompletedTask);

		// Act
		await _taskService.DeleteTaskAsync(taskId);

		// Assert
		repo.Verify(x => x.DeleteAsync(taskId, It.IsAny<CancellationToken>()), Times.Once);
		_mockUnitOfWork.Verify(x => x.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
	}
}
