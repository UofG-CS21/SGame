"""
conftest.py: pytest configuration and fixtures.
"""

import os, sys
import pytest
import requests
import subprocess as sp
from time import sleep


def pytest_addoption(parser):
    """
    This configures the commandline arguments that can be passed to pytest.
    """
    parser.addoption("--sgame", action="store", default="../SGame", type=str, help="Path to the SGame source directory")
    parser.addoption("--host", action="store", default='localhost', type=str, help="Host to bind the SGame server instance to")
    parser.addoption("--port", action="store", default=5000, type=int, help="Port to bind the SGame server instance to")


# Test fixtures


class ServerFixture:
    """A running instance of SGame."""

    def __init__(self, host: int, port: str):
        self.host = str(host)
        """The host the server is bound to."""
        self.port = int(port)
        """The port the server is bound to."""

    @property
    def url(self) -> str:
        """Returns the base URL for the REST API ("http://<host>:<port>/")"""
        return f'http://{self.host}:{self.port}/'


@pytest.fixture(scope='session')
def server(request) -> ServerFixture:
    """
    A session-wide fixture that starts an instance of the SGame server in the background
    (and kills it when the testing session is done).
    """
    sgame_root, host, port = map(request.config.getoption, ["--sgame", "--host", "--port"])
    sgame_dir, sgame_name = os.path.split(os.path.realpath(sgame_root))

    # Start the SGame compute node in the background
    cmd = ['dotnet', 'run', '--project', sgame_name, '--', '--host', host, '--port', str(port)]
    print(f'-- Starting SGame instance: `{" ".join(cmd)}`', file=sys.stderr)
    server_proc = sp.Popen(cmd, stdout=sys.STDERR, stderr=sys.STDERR)

    # Wait for a bit for the server to startup
    sleep(5)

    yield ServerFixture(host, port)

    # Kill the background process when testing is done
    print('-- Killing server', file=sys.stderr)
    server_proc.kill()


class ClientFixture:
    """The state of a connected ship."""

    def __init__(self, id: int, token: str, url: str):
        self.id = int(id)
        """The connected ship's ID."""
        self.token = str(token)
        """The connected ship's token."""
        self.url = str(url)
        """The REST API URL this client connected to."""


@pytest.fixture
def client(server) -> ClientFixture:
    """
    A test fixture that calls the `connect` REST API and gets a ship id / token pair.
    """
    resp = requests.post(url=server.url + 'connect')
    if not resp: raise RuntimeError(f"Could not connect to server at {server.url}!")

    resp_dict = resp.json()
    if 'id' not in resp_dict: raise RuntimeError("Ship id not present in `connect` response!")
    if 'token' not in resp_dict: raise RuntimeError("Ship token not present in `connect` response!")

    return ClientFixture(int(resp_dict['id']), resp_dict['token'], server.url)
