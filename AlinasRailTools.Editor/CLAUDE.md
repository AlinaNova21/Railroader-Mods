# AlinasRailTools.Editor - Claude Code Guidelines

## Coding Standards

### Logging
- **NEVER use `Console.WriteLine`** - Always use Serilog logger instead
- Use `ILogger _logger = Log.ForContext<ClassName>()` in class constructors
- Example:
  ```csharp
  private readonly ILogger _logger;

  public MyClass()
  {
      _logger = Log.ForContext<MyClass>();
      _logger.Information("Initialized");
  }
  ```

### Architecture
- **ECS-based**: Uses Arch ECS for entity management
- **Veldrid rendering**: Low-level graphics API for terrain and track rendering
- **ImGui UI**: All UI is built with ImGui.NET

### Performance Guidelines
- **Terrain tiles**: Process max 50 tiles per frame
- **Instance rendering**: Use single reusable GPU buffer that grows as needed
- **Background threads**: Move CPU-intensive work (heightmap conversion) off main thread
- **Frame budgets**:
  - Target 60 FPS (16.67ms per frame)
  - Warn below 30 FPS (33ms per frame)

### Code Organization
- Use file-scoped namespaces
- Keep systems in `ECS/Systems/`
- Keep components in `ECS/Components/`
- Keep rendering code in `Rendering/`
