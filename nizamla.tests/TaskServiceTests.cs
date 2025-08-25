using Xunit;
using Moq;
using AutoMapper;
using nizamla.Application.Services;
using nizamla.Core.Entities;
using nizamla.Core.Interfaces;
using nizamla.Application.Mappings;
using nizamla.Application.dtos;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace nizamla.Tests
{
    public class TaskServiceTests
    {
        private readonly Mock<ITaskRepository> _taskRepositoryMock;
        private readonly IMapper _mapper;
        private readonly TaskService _taskService;

        public TaskServiceTests()
        {
            _taskRepositoryMock = new Mock<ITaskRepository>();

            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<TaskMappingProfile>();
            });
            _mapper = config.CreateMapper();

            _taskService = new TaskService(_taskRepositoryMock.Object, _mapper);
        }

        [Fact]
        public async Task GetAllTasksAsync_ShouldReturnTasks()
        {
            // Arrange
            var tasks = new List<TaskItem>
            {
                new TaskItem { Id = 1, Title = "Task 1" },
                new TaskItem { Id = 2, Title = "Task 2" }
            };
            _taskRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(tasks);

            // Act
            var result = await _taskService.GetAllTasksAsync();

            // Assert
            Assert.Equal(2, result.Count());
            Assert.Contains(result, t => t.Title == "Task 1");
        }
        
        [Fact]
        public async Task GetTaskByIdAsync_ShouldReturnTask_WhenExists()
        {
            // Arrange
            var task = new TaskItem { Id = 1, Title = "Test Task" };
            _taskRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(task);

            // Act
            var result = await _taskService.GetTaskByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test Task", result.Title);
        }

        [Fact]
        public async Task GetTaskByIdAsync_ShouldReturnNull_WhenNotExists()
        {
            // Arrange
            _taskRepositoryMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((TaskItem?)null);

            // Act
            var result = await _taskService.GetTaskByIdAsync(99);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task CreateTaskAsync_ShouldReturnCreatedTask()
        {
            // Arrange
            var createDto = new CreateTaskDto { Title = "New Task", Description = "Desc" };
            var task = new TaskItem { Id = 1, Title = "New Task", Description = "Desc" };

            _taskRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<TaskItem>())).ReturnsAsync(task);

            // Act
            var result = await _taskService.CreateTaskAsync(createDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("New Task", result.Title);
        }

        [Fact]
        public async Task DeleteTaskAsync_ShouldReturnTrue_WhenDeleted()
        {
            // Arrange
            _taskRepositoryMock.Setup(r => r.DeleteAsync(1)).ReturnsAsync(true);

            // Act
            var result = await _taskService.DeleteTaskAsync(1);

            // Assert
            Assert.True(result);
        }
    }
}
