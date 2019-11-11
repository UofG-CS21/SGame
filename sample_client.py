import requests
import argparse

parser = argparse.ArgumentParser()
parser.add_argument('-P', '--port', type=int, default = 8000, help='Port to connect to')
parser.add_argument('-H', '--host', type=str, default = 'localhost', help = 'Host to connect to')

args = parser.parse_args()
port = getattr(args,'port')
host = getattr(args,'host')

print('Sending connect request...')
response = requests.post(url='http://' + host + ':' + str(port) + '/connect')
data = {'token' : response.json()['token']}
print('Received session token ' + response.json()['token'])
print('Sending disconnect request...')
response = requests.post(url = 'http://' + host + ':' + str(port) + '/disconnect', json = data)
print('Received response: ' + response.text)
