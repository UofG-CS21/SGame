import requests
import argparse
import time

parser = argparse.ArgumentParser()
parser.add_argument('-P', '--port', type=int, default = 5000, help='Port to connect to')
parser.add_argument('-H', '--host', type=str, default = 'localhost', help = 'Host to connect to')

args = parser.parse_args()
port = getattr(args,'port')
host = getattr(args,'host')

def APICall(token, API, data = {}):
    data['token'] = token
    response = requests.post(url='http://' + host + ':' + str(port) + '/' + API,json = data)
    return response

print('Sending connect request A...')
response = requests.post(url='http://' + host + ':' + str(port) + '/connect')
Atoken = response.json()['token']
print('Received session token ' + Atoken)

print('Sending connect request B...')
response = requests.post(url='http://' + host + ':' + str(port) + '/connect')
Btoken = response.json()['token']
print('Received session token ' + Btoken)

print('Moving player A to the left...')
APICall(Atoken,'accelerate',{'x':1,'y':0,})
time.sleep(1)
APICall(Atoken,'accelerate',{'x':-1,'y':0})

print('Player A is now:')
print(APICall(Atoken,'getShipInfo').text)

print('Scanning for player A...')
response = APICall(Btoken,'scan',{'direction':180,'width':30,'energy':5})
print('Received response: ' + response.text)

print('Sending disconnect request A...')
response = APICall(Atoken, 'disconnect')
print('Received response: ' + response.text)

print('Sending disconnect request B...')
response = APICall(Btoken, 'disconnect')
print('Received response: ' + response.text)
