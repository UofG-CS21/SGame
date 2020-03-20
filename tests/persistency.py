import requests
import pytest

allowed_fpe = 1e-6

def test_basic_persistency(persistency):
    if not persistency:
        pytest.skip("--persistency tests not enabled by user")
