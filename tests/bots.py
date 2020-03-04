import requests
import time
import abc
from multiprocessing import Process, Pipe

class Bot:

    def __init__(self,server):
        self.host = server.host
        self.port = server.port
        resp = requests.post(url='http://' + self.host + ':' + str(self.port) + '/connect')
        self.token = resp.json()['token']
        self.maxHangTime = {}
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
        self.maxHangTime[path] = max(self.maxHangTime.get(path,0),end-start)
        if path not in self.apiCallTimes:
            self.apiCallTimes[path] = {'calls':0,'time':0}
        self.apiCallTimes[path]['calls'] += 1
        self.apiCallTimes[path]['time'] += end-start
        return (resp,end-start)

    def generate_output(self):
        return 1

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
        output.send(self.generate_output())

# disconnects and connects {rate} times a second
class Discobot(Bot):
    def __init__(self, server, rate = 3):
        super().__init__(server)
        self.rate = rate
        self.name = "Discobot"

    def generate_output(self,):
        return (self.token, self.maxHangTime, self.apiCallTimes)

    def run_inner(self, output, t = 3):
        start = time.time()
        delay = 1.0 / self.rate
        last = start
        while time.time()-start < t:
            assert self.timedapi('disconnect')
            resp = self.timedapi('connect')
            self.token = resp[0].json()['token']
            wait = delay - (time.time() - last)
            last = time.time()
            if wait > 0:
                time.sleep(wait)
        
        output.send(self.generate_output())

    def finish(self):
        if self.p is None:
            print("You need to run " + self.name + " before you finish!")
            return False
        self.p.join()
        self.token, self.maxHangTime, self.apiCallTimes = self.pipe[1].recv()
        res = self.quit()
        return res
