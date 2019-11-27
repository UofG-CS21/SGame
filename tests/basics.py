import requests


def test_disconnect(client):
    """
    Tests that a connected ship can disconnect via REST.
    """
    resp = requests.post(client.url + 'disconnect',
                         json={'token': client.token})
    assert resp


def test_movement(client):
    # Test values for the coordinates
    x = 23.6
    y = 45.5
    # Testing accelerate with the given x and y values
    resp = requests.post(client.url + 'accelerate', json={
        'token': client.token,
        'x': x,
        'y': y,
    })
    assert resp
    # Using the getShipInfo to test the values are correct
    resp = requests.post(client.url + 'getShipInfo', json={
        'token': client.token,
    })
    assert resp

    resp_data = resp.json()
    assert resp_data['x'] == x
    assert resp_data['y'] == y

    # Disconnect
    resp = requests.post(client.url + 'disconnect',
                         json={'token': client.token})
    assert resp
