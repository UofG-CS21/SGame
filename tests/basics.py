import pytest
import requests


#def test_disconnect(client):
#    """
#    Tests that a connected ship can disconnect via REST.
#    """
#    resp = requests.post(client.url + 'disconnect', json={'token': client.token})
#    assert resp

def test_basic():
    """
    A basic test that always succeeds.
    """
    assert (1 + 1) == 2
