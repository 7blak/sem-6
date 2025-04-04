cmake_minimum_required(VERSION 3.16)

project(main LANGUAGES C)

add_executable(main main.c)

include(GNUInstallDirs)
install(TARGETS main
    LIBRARY DESTINATION ${CMAKE_INSTALL_LIBDIR}
    RUNTIME DESTINATION ${CMAKE_INSTALL_BINDIR}
)
