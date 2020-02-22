"""
conftest.py: pytest configuration and fixtures.
"""

import os
import sys
import pytest
import requests
import subprocess as sp
from typing import Iterable
from time import sleep


def pytest_addoption(parser):
    """
    This configures the commandline arguments that can be passed to pytest.
    """
    parser.addoption("--sgame", action="store", default="../SGame",
                     type=str, help="Path to the SGame source directory")
    parser.addoption("--host", action="store", default='localhost',
                     type=str, help="Host to bind the SGame server instance to")
    parser.addoption("--port", action="store", default=5000,
                     type=int, help="Port to bind the SGame server instance to")


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
    A session-wide fixture passed to know the host/port of the SGame server started by GitLab.
    """
    sgame_root, host, port = map(request.config.getoption, [
                                 "--sgame", "--host", "--port"])
    sgame_dir, sgame_name = os.path.split(os.path.realpath(sgame_root))

    yield ServerFixture(host, port)


class Client:
    """The state of a connected ship."""

    def __init__(self, token: str, url: str):
        self.token = token
        """The connected ship's token."""
        self.id = self.token[-8:]
        """The connected ship's public ID."""
        self.url = url
        """The REST API URL this client connected to."""


class ClientsFixture:
    """A fixture that manages a number of clients to connect."""

    def __init__(self, server: ServerFixture):
        self.server = server

    def __call__(self, n_clients: int) -> 'self':
        self.n_clients = n_clients
        return self

    def __enter__(self) -> Iterable[Client]:
        """Connects `n_clients` clients.
        Returns either a list of all connected `Client`s or just the single `Client`
        that was connected if `n_clients` was 1."""

        self.clients = []
        for i in range(self.n_clients):
            resp = requests.post(url=self.server.url + 'connect')
            if not resp:
                raise RuntimeError(
                    f"Could not connect to server at {self.server.url}!")

            resp_dict = resp.json()
            if 'token' not in resp_dict:
                raise RuntimeError(
                    "Ship token not present in `connect` response!")
            self.clients.append(
                Client(resp_dict['token'], self.server.url))

        if len(self.clients) > 1:
            return self.clients
        else:
            return self.clients[0]

    def __exit__(self, type, value, traceback):
        """Disconnect all clients."""
        for client in self.clients:
            requests.post(url=self.server.url + 'disconnect',
                          json={'token': client.token})
        self.clients = []
        return False  # Raise any exceptions back to caller


@pytest.fixture
def clients(server) -> ClientsFixture:
    """
    Returns a callable that, when given the number `n` of clients to create as parameter,
    connects `n` clients.
    ```
    def my_test(clients):
        with client1, client2 as clients(2):
            <do stuff with the clients>
    ```
    """
    return ClientsFixture(server)  # Acts as a callable but it's actually a class...
