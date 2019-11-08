
import pytest


def pytest_addoption(parser):
    parser.addoption(
        "--host", action="store", default="localhost", help="The hostname for the server"
    )
    parser.addoption(
        "--port", action="store", default=5000, type=int, help="The port of the sever we are connecting to"
    )


@pytest.fixture
def host(request):
    return request.config.getoption("--host")


@pytest.fixture
def port(request):
    return request.config.getoption("--port")
