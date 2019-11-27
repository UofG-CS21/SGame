import requests


def test_disconnect(client):
    """
    Tests that a connected ship can disconnect via REST.
    """
    resp = requests.post(client.url + 'disconnect',
                         json={'token': client.token})
    assert resp


def test_movement(client):
    resp = requests.post(client.url + 'accelerate', json={
        'token': client.token,
        'x': 23.6,
        'y': 45.5,
    })
    assert resp

    # TODO:  Check if the acceleration matches the expected position, and add comments

    resp = requests.post(client.url + 'disconnect',
                         json={'token': client.token})
    assert resp
