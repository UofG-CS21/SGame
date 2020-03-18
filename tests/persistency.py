import requests
import time
import pytest

allowed_fpe = 1e-6

@pytest.mark.skipif(sys.platform == "win32", reason="does not run on windows")
def test_persistency():
    pass

