"""
conftest.py: pytest configuration and fixtures.
"""

import pytest
import requests


def pytest_addoption(parser):
    """
    This configures the commandline arguments that can be passed to pytest.
    """
    parser.addoption("--host", action="store", default="localhost", help="The hostname for the server")
    parser.addoption("--port", action="store", default=5000, type=int, help="The port of the sever we are connecting to")


# Test fixtures

class ShipFixture:
    """The state of a connected ship."""

    def __init__(self, host: str, port: int, id: int, token: str):
        self.host = str(host)
        """The REST server's hostname."""
        self.port = int(port)
        """The REST server's port."""
        self.id = int(id)
        """The connected ship's ID."""
        self.token = str(token)
        """The connected ship's token."""

    @property
    def url(self) -> str:
        """Returns the base URL for the REST API ("http://<host>:<port>/")"""
        return f'http://{self.host}:{self.port}/'


@pytest.fixture
def client(request) -> ShipFixture:
    """
    A test fixture that fetches host and port command-line arguments, then calls the `connect` REST API and gets a ship
    id / token pair.
    """
    host, port = map(request.config.getoption, ["--host", "--port"])

    resp = requests.post(url=f'http://{host}:{port}/connect')
    if not resp: raise RuntimeError(f"Could not connect to server at {host}:{port}!")

    resp_dict = resp.json()
    if 'id' not in resp_dict: raise RuntimeError("Ship id not present in `connect` response!")
    if 'token' not in resp_dict: raise RuntimeError("Ship token not present in `connect` response!")

    return ShipFixture(host, port, int(resp_dict['id']), resp_dict['token'])
