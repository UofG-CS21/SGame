import pytest
import requests


def test_disconnect(client):
    """
    Tests that a connected ship can disconnect via REST.
    """
    resp = requests.post(client.url + 'disconnect', {'token': client.token})
    assert resp
