# GitHub Workflows Documentation

# GitHub 工作流文档

This directory contains GitHub Actions workflows for the LiveWordXml project.
此目录包含 LiveWordXml 项目的 GitHub Actions 工作流。

## Available Workflows / 可用的工作流

### 1. CI Workflow (`ci.yml`)

**Purpose / 用途**: Continuous Integration for daily development
**用途**: 日常开发的持续集成

**Triggers / 触发条件**:

- Push to any branch / 推送到任何分支
- Pull requests to main/master / 针对 main/master 分支的 pull request

**What it does / 功能**:

- ✅ Builds the solution / 构建解决方案
- ✅ Runs unit tests / 运行单元测试
- ✅ Generates test coverage reports / 生成测试覆盖率报告
- ✅ Uploads test results as artifacts / 上传测试结果作为产物

### 2. Build and Release Workflow (`build-and-release.yml`)

**Purpose / 用途**: Complete build, test, and release pipeline
**用途**: 完整的构建、测试和发布流水线

**Triggers / 触发条件**:

- Push to main/master branch / 推送到 main/master 分支
- Pull requests to main/master / 针对 main/master 分支的 pull request
- Tag creation (v*) / 创建标签 (v*)

**What it does / 功能**:

- ✅ Builds and tests the application / 构建和测试应用程序
- ✅ Publishes for multiple Windows platforms / 为多个 Windows 平台发布
  - Windows x64 (64-bit)
  - Windows x86 (32-bit)
  - Windows ARM64
- ✅ Creates GitHub releases with downloadable assets / 创建包含可下载资源的 GitHub 发布
- ✅ Automatic cleanup of build artifacts / 自动清理构建产物

## How to Use / 使用方法

### For Daily Development / 日常开发

1. Push your code to any branch / 将代码推送到任何分支
2. The CI workflow will automatically run / CI 工作流将自动运行
3. Check the Actions tab to see build status / 查看 Actions 标签页以查看构建状态

### For Releases / 发布版本

1. Create and push a tag with version format / 创建并推送版本格式的标签:
   ```bash
   git tag v1.0.0
   git push origin v1.0.0
   ```
2. The release workflow will automatically:
   发布工作流将自动:
   - Build the application for all platforms / 为所有平台构建应用程序
   - Create a GitHub release / 创建 GitHub 发布
   - Upload downloadable ZIP files / 上传可下载的 ZIP 文件

### Monitoring Workflows / 监控工作流

- Go to the **Actions** tab in your GitHub repository
- 转到 GitHub 仓库中的 **Actions** 标签页
- View workflow runs, logs, and artifacts
- 查看工作流运行、日志和产物

## Configuration / 配置

### Environment Variables / 环境变量

Both workflows use these environment variables:
两个工作流都使用这些环境变量:

- `DOTNET_VERSION`: .NET SDK version (currently 8.0.x)
- `BUILD_CONFIGURATION`: Build configuration (Release)
- `PROJECT_PATH`: Main project path
- `TEST_PROJECT_PATH`: Test project path

### Secrets (Optional) / 密钥（可选）

For enhanced functionality, you can add these secrets in repository settings:
为了增强功能，您可以在仓库设置中添加这些密钥:

- `CODECOV_TOKEN`: For code coverage reporting / 用于代码覆盖率报告

## Customization / 自定义

### Adding New Platforms / 添加新平台

To add support for additional platforms, modify the `runtime` matrix in `build-and-release.yml`:
要添加对其他平台的支持，请修改 `build-and-release.yml` 中的 `runtime` 矩阵:

```yaml
strategy:
  matrix:
    runtime: [win-x64, win-x86, win-arm64, linux-x64, osx-x64]
```

### Changing Trigger Conditions / 更改触发条件

Modify the `on` section in workflow files to change when workflows run:
修改工作流文件中的 `on` 部分以更改工作流运行时机:

```yaml
on:
  push:
    branches: [main, develop]
  schedule:
    - cron: "0 2 * * 1" # Weekly on Monday at 2 AM
```

## Troubleshooting / 故障排除

### Common Issues / 常见问题

1. **Build Failures / 构建失败**

   - Check the build logs in the Actions tab
   - 在 Actions 标签页中查看构建日志
   - Ensure all dependencies are properly referenced
   - 确保所有依赖项都正确引用

2. **Test Failures / 测试失败**

   - Review test output in the workflow logs
   - 在工作流日志中查看测试输出
   - Run tests locally to reproduce issues
   - 在本地运行测试以重现问题

3. **Release Issues / 发布问题**
   - Verify tag format matches `v*` pattern
   - 验证标签格式是否匹配 `v*` 模式
   - Check repository permissions for releases
   - 检查仓库的发布权限

### Getting Help / 获取帮助

- Check GitHub Actions documentation
- 查看 GitHub Actions 文档
- Review workflow logs for detailed error messages
- 查看工作流日志以获取详细错误信息
- Open an issue in the repository for project-specific problems
- 为项目特定问题在仓库中开启 issue
