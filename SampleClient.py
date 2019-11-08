import requests

print('Sending connect request...')
response = requests.post(url='http://localhost:8000/connect')
data = {'token' : response.json()['token']}
print('Received session token ' + response.json()['token'])
print('Sending disconnect request...')
response = requests.post(url = 'http://localhost:8000/disconnect', json = data)
print('Received response: ' + response.text)
