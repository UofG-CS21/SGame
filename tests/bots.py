import requests
import time
import abc
import random
import string
from multiprocessing import Process, Pipe

# functions to generate random json data for API requests

def GSIgen():
    return {}

def accgen():
    return {'x': random.randrange(-5,5), 'y':random.randrange(-5,5)}

def scangen():
    return {'width':random.randrange(1,90),'direction':random.randrange(-345,456),'energy':random.randrange(1,10)}

def shootgen():
    return {'width':random.randrange(1,90),'direction':random.randrange(-345,456),'energy':random.randrange(1,10),'damage':0.000001}

def shieldgen():
    return {'width':random.randrange(1,180),'direction':random.randrange(-345,456)}

API = [ ['shoot', shootgen], ['scan', scangen], ['getShipInfo', GSIgen], ['accelerate', accgen], ['shield', shieldgen] ]

def randomAPI():
    api = random.choice(API)
    return api[0], api[1]()

class Bot:

    def __init__(self,server):
        self.host = server.host
        self.port = server.port
        resp = requests.post(url='http://' + self.host + ':' + str(self.port) + '/connect')
        self.token = resp.json()['token']
        self.apiCallTimes = {}
        self.p = None
        self.pipe = None
        self.name = "Bot"

    #def api(self, path, data):
    #    data['token'] = self.token
    #    return requests.post(url = 'http://' + self.host + ':' + str(port) + '/' + path, json=data)

    def quit(self):
        data = { 'token': self.token }
        return requests.post(url = 'http://' + self.host + ':' + str(self.port) + '/disconnect', json=data)

    def timedapi(self, path, data = {}):
        data['token'] = self.token
        start = time.time()
        resp = requests.post(url = 'http://' + self.host + ':' + str(self.port) + '/' + path, json=data)
        end = time.time()
        if path not in self.apiCallTimes:
            self.apiCallTimes[path] = []
        self.apiCallTimes[path].append(end-start)
        return (resp,end-start)

    def generate_output(self,word="basic"):
        return "I am a " + word + " bot and did nothing succesfully"

    def run_inner(self, output, t = 3):
        output.send(self.generate_output())

    def run(self, t = 3):
        self.pipe = Pipe()
        self.p = Process(target=self.run_inner, args = (self.pipe[0],t,))
        self.p.daemon = True
        self.p.start()

    def finish(self):
        if self.p is None:
            print("You need to run " + self.name + " before you finish!")
            return False
        self.p.join()
        #ignoring
        assert self.pipe[1].recv()
        return self.quit()

# just chills
class Idlebot(Bot):
    def __init__(self, server):
        super().__init__(server)
        self.name = "Idlebot"

    def run_inner(self, output, t = 3):
        time.sleep(t)
        output.send(self.generate_output('idle'))

# uses a random command {rate} times a second
class Randombot(Bot):
    def __init__(self, server, rate = 3):
        super().__init__(server)
        self.rate = rate
        self.name = 'Randombot'

    def generate_output(self):
        return (self.apiCallTimes)

    def run_inner(self, output, t = 3):
        calls = 0
        start = time.time()
        delay = 1.0 / self.rate
        while time.time()-start < t:
            calls += 1
            last = time.time()
            path, data = randomAPI()
            res = self.timedapi(path, data)
            assert res[0]
            #if not res[0]:
            #    print('oops',path,data,res[0].json())
            wait = delay - (time.time() - last)
            if wait > 0:
                time.sleep(wait)
        output.send(self.generate_output())

    def finish(self):
        if self.p is None:
            print("You need to run " + self.name + " before you finish!")
            return False
        self.p.join()
        self.apiCallTimes = self.pipe[1].recv()
        res = self.quit()
        return res


# disconnects and connects {rate} times a second
class Discobot(Bot):
    def __init__(self, server, rate = 3):
        super().__init__(server)
        self.rate = rate
        self.name = "Discobot"

    def generate_output(self,):
        return (self.token, self.apiCallTimes)

    def run_inner(self, output, t = 3):
        calls = 0
        start = time.time()
        delay = 1.0 / self.rate
        while time.time()-start < t:
            calls += 1
            last = time.time()
            assert self.timedapi('disconnect')[0]
            resp = self.timedapi('connect')
            assert resp[0]
            self.token = resp[0].json()['token']
            wait = delay - (time.time() - last)
            if wait > 0:
                time.sleep(wait)
        output.send(self.generate_output())

    def finish(self):
        if self.p is None:
            print("You need to run " + self.name + " before you finish!")
            return False
        self.p.join()
        self.token, self.apiCallTimes = self.pipe[1].recv()
        res = self.quit()
        return res

# sends random garbage API
class Iliteratebot(Bot):
    def __init__(self, server, rate):
        super().__init__(server)
        self.name = 'Iliteratebot'
        self.rate = rate

    def generate_output(self,):
        return (self.apiCallTimes)


    def run_inner(self, output, t = 3):
        start = time.time()
        delay = 1.0 / self.rate
        garbage = ''.join([random.choice(string.ascii_letters + string.digits) for n in range(10)])
        garbage += '!' # to make sure it is an invalid command
        while time.time()-start < t:
            last = time.time()
            resp = self.timedapi(garbage)
            assert not resp[0]
            assert 'error' in resp[0].json().keys()
            wait = delay - (time.time() - last)
            if wait > 0:
                time.sleep(wait)
        output.send(self.generate_output())

    def finish(self):
        if self.p is None:
            print("You need to run " + self.name + " before you finish!")
            return False
        self.p.join()
        self.apiCallTimes = self.pipe[1].recv()
        return self.quit()

# sends no data other than its token
class GDPRbot(Bot):
    def __init__(self, server, rate):
        super().__init__(server)
        self.name = 'GDPRbot'
        self.rate = rate

    def generate_output(self,):
        return (self.apiCallTimes)

    def run_inner(self, output, t = 3):
        start = time.time()
        delay = 1.0 / self.rate
        while time.time()-start < t:
            last = time.time()
            path, data = randomAPI()
            res = self.timedapi(path, {})
            if path == 'getShipInfo':
                assert res[0]
            else:
                assert not res[0]
                assert 'error' in res[0].json().keys()
            wait = delay - (time.time() - last)
            if wait > 0:
                time.sleep(wait)
        output.send(self.generate_output())

    def finish(self):
        if self.p is None:
            print("You need to run " + self.name + " before you finish!")
            return False
        self.p.join()
        self.apiCallTimes = self.pipe[1].recv()
        res = self.quit()
        return res

# so anyway I started blasting
class Spambot(Bot):
    def __init__(self, server):
        super().__init__(server)
        self.name = 'Spambot'
        self.spams = 0

    def generate_output(self,):
        return (self.spams)

    def run_inner(self, output, t = 3):
        start = time.time()
        while time.time()-start < t:
            path,data = randomAPI()
            data['token'] = self.token
            requests.post(url = 'http://' + self.host + ':' + str(self.port) + '/' + path, json=data)
            self.spams += 1
        output.send(self.generate_output())

    def finish(self):
        if self.p is None:
            print("You need to run " + self.name + " before you finish!")
            return False
        self.p.join()
        self.spams = self.pipe[1].recv()
        res = self.quit()
        return res

# sends massive Jsons
class Yuugebot(Bot):
    def __init__(self, server, numBytes=1024, rate = 3):
        super().__init__(server)
        self.rate = rate
        self.name = 'Yuugebot'
        self.bytes = numBytes
        self.payload = 0

    def generate_output(self):
        return (self.apiCallTimes,self.payload)

    def run_inner(self, output, t = 3):

        calls = [ randomAPI() for i in range(10) ]
        for call in range(10):
            calls[call][1]['gift'] = ''.join([random.choice(string.ascii_letters + string.digits) for n in range(self.bytes)])

        start = time.time()
        delay = 1.0 / self.rate

        while time.time()-start < t:
            last = time.time()
            choose = random.randrange(0,10)
            res = self.timedapi(calls[choose][0], calls[choose][1])
            self.payload += self.bytes
            assert res[0]
            wait = delay - (time.time() - last)
            if wait > 0:
                time.sleep(wait)
        output.send(self.generate_output())

    def finish(self):
        if self.p is None:
            print("You need to run " + self.name + " before you finish!")
            return False
        self.p.join()
        self.apiCallTimes,self.payload = self.pipe[1].recv()
        res = self.quit()
        return res

# tries to run away as far as possible
class Coronavirusbot(Bot):
    def __init__(self, server, rate = 3):
        super().__init__(server)
        self.rate = rate
        self.name = "Coronavirusbot"

    def generate_output(self,):
        return (self.apiCallTimes)

    def run_inner(self, output, t = 3):
        x,y = random.uniform(-1,1)*47,random.random(-1,1)*47
        start = time.time()
        data = {'x':x,'y':y}
        delay = 1.0 / self.rate
        while time.time()-start < t:
            calls += 1
            last = time.time()
            resp = self.timedapi('accelerate', data)
            assert resp[0]
            wait = delay - (time.time() - last)
            if wait > 0:
                time.sleep(wait)
        output.send(self.generate_output())

    def finish(self):
        if self.p is None:
            print("You need to run " + self.name + " before you finish!")
            return False
        self.p.join()
        self.token, self.apiCallTimes = self.pipe[1].recv()
        res = self.quit()
        return res
