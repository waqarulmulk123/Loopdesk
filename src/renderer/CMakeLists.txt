cmake_minimum_required(VERSION 3.20)
project(LiveWallpaperRenderer CXX)

set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

if(NOT WIN32)
  message(FATAL_ERROR "The native renderer targets Windows only. Build it on Windows or in Windows CI.")
endif()

add_executable(lws-renderer
  src/main.cpp
  src/DesktopShellAdapter.cpp
  src/DisplayManager.cpp
  src/Dx11Device.cpp
  src/PlaybackPolicy.cpp
  src/DesktopWindow.cpp
  src/VideoMediaFoundation.cpp
  src/SwapChainPresenter.cpp
  src/IpcServer.cpp
)

target_include_directories(lws-renderer PRIVATE include)
target_link_libraries(lws-renderer PRIVATE user32 gdi32 dxgi d3d11 mfplat mf ole32 shcore)
target_compile_definitions(lws-renderer PRIVATE UNICODE _UNICODE)

if(MSVC)
  target_compile_options(lws-renderer PRIVATE /W4 /WX-)
endif()
