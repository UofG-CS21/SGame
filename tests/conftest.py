
import pytest


def pytest_addoption(parser):
    """This configures the commandline arguments that can be passed to test files by pytest"""
    parser.addoption(
        "--host", action="store", default="localhost", help="The hostname for the server"
    )
    parser.addoption(
        "--port", action="store", default=5000, type=int, help="The port of the sever we are connecting to"
    )

# Getter methods


@pytest.fixture
def host(request):
    return request.config.getoption("--host")


@pytest.fixture
def port(request):
    return request.config.getoption("--port")
