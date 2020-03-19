import requests
import time
import pytest
import bots

def getMaxHangTime(bots):
    return max( [ max([ max(times) for times in bot.apiCallTimes.values()]) for bot in bots ] )

def test_init(server):
    bot1 = bots.Bot(server)
    bot1.run(3)
    bot2 = bots.Bot(server)
    bot2.run(3)

    assert bot1.finish()
    assert bot2.finish()

def test_idle(server):
    bot1 = bots.Idlebot(server)
    bot2 = bots.Idlebot(server)

    bot1.run(3)
    bot2.run(3)
    assert bot1.finish()
    assert bot2.finish()

basic_data = [
    # FORMAT: numBots timeSec callsPerSec
    (10,5,50)
]

# random valid API calls
@pytest.mark.parametrize("numBots, timeSec, callsPerSec", basic_data)
def test_random(server,numBots,timeSec,callsPerSec):
    randombots = [ bots.Randombot(server, callsPerSec) for i in range(numBots) ]
    for i in range(numBots):
        randombots[i].run(timeSec)
    for i in range(numBots):
        assert randombots[i].finish() 

    MHT = getMaxHangTime(randombots)
    print('Highest wait time for random',numBots,timeSec,callsPerSec,'=',str(MHT))

# connect/disconnect
@pytest.mark.parametrize("numBots, timeSec, callsPerSec", basic_data)
def test_disco(server, numBots, timeSec, callsPerSec):
    discobots = [ bots.Discobot(server, callsPerSec) for i in range(numBots) ]
    for i in range(numBots):
        discobots[i].run(timeSec)
    for i in range(numBots):
        assert discobots[i].finish() 

    MHT = max([ max(discobots[i].apiCallTimes['connect']) for i in range(numBots) ])
    print('Highest wait time for connect',numBots,timeSec,callsPerSec,'=',str(MHT))

# random garbage
@pytest.mark.parametrize("numBots, timeSec, callsPerSec", basic_data)
def test_iliterate(server, numBots, timeSec, callsPerSec):
    iliteratebots = [ bots.Iliteratebot(server, callsPerSec) for i in range(numBots) ]
    for i in range(numBots):
        iliteratebots[i].run(timeSec)
    for i in range(numBots):
        assert iliteratebots[i].finish() 

    MHT = getMaxHangTime(iliteratebots)
    print('Highest wait time for invalid API route',numBots,timeSec,callsPerSec,'=',str(MHT))


# empty data
@pytest.mark.parametrize("numBots, timeSec, callsPerSec", basic_data)
def test_GDPR(server,numBots,timeSec,callsPerSec):
    GDPRbots = [ bots.GDPRbot(server, callsPerSec) for i in range(numBots) ]
    for i in range(numBots):
        GDPRbots[i].run(timeSec)
    for i in range(numBots):
        assert GDPRbots[i].finish() 

    MHT = getMaxHangTime(GDPRbots)
    print('Highest wait time for GDPR',numBots,timeSec,callsPerSec,'=',str(MHT))

# random bots with a spammer
@pytest.mark.parametrize("numBots, timeSec, callsPerSec", basic_data)
def test_random_spammed(server,numBots,timeSec,callsPerSec):
    randombots = [ bots.Randombot(server, callsPerSec) for i in range(numBots) ]
    for i in range(numBots):
        randombots[i].run(timeSec)
    spambot = bots.Spambot(server)
    spambot.run(timeSec)
    for i in range(numBots):
        assert randombots[i].finish() 
    assert spambot.finish()
    MHT = getMaxHangTime(randombots)
    print('Highest wait time for random',numBots,timeSec,callsPerSec,', while spambot spammed',str(spambot.spams),'=',str(MHT))

# random bots with a yuuge bot
@pytest.mark.parametrize("numBots, timeSec, callsPerSec", basic_data)
def test_random_yuuged(server,numBots,timeSec,callsPerSec):
    randombots = [ bots.Randombot(server, callsPerSec) for i in range(numBots) ]
    yuugebot = bots.Yuugebot(server, 1024, callsPerSec)
    yuugebot.run(timeSec)
    for i in range(numBots):
        randombots[i].run(timeSec)
    for i in range(numBots):
        assert randombots[i].finish() 
    assert yuugebot.finish()
    MHT = getMaxHangTime(randombots)
    MHTyuuge = getMaxHangTime([yuugebot])
    print('Highest wait time for random',numBots,timeSec,callsPerSec,'=',str(MHT),'while yuugebot had',str(MHTyuuge),'with total payload',str(yuugebot.payload),'bytes')

# random bots with accelerators
@pytest.mark.parametrize("numBots, timeSec, callsPerSec", basic_data)
def test_random_corona(server,numBots,timeSec,callsPerSec):
    randombots = [ bots.Randombot(server, callsPerSec) for i in range(numBots//2) ]
    coronabots = [ bots.Yuugebot(server, callsPerSec) for i in range(numBots-numBots//2) ]
    for i in range(len(randombots)):
        randombots[i].run(timeSec)
    for i in range(len(coronabots)):
        coronabots[i].run(timeSec);
    for i in range(len(randombots)):
        assert randombots[i].finish()
    for i in range(len(coronabots)):
        assert coronabots[i].finish() 

    MHT = getMaxHangTime(randombots)
    MHTcorona = getMaxHangTime(coronabots)
    print('Highest wait time for random',numBots,timeSec,callsPerSec,'=',str(MHT),'while accelerators had',str(MHTcorona))
