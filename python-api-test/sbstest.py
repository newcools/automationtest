import requests
import json

# this test code is below "production quality" as not little info on requirement is provided. --anyway, best effort 
def test_sbs_api_example():
    url = 'https://www.sbs.com.au/guide/ajax_radio_program_catchup_data/language/greek/location/NSW/sublocation/Sydney'
    
    resp = requests.get(url)
    assert resp.status_code == 200
    assert resp.headers["Content-Type"] == "application/json"
    resp_body = resp.json()
    
    # test if all the field is in place, 
    # TODO: this really depends on the API spec, which is missing, improve later
    for pd in resp_body:
        assert {"label", "program", "channelName", "startTime", \
            "endTime", "onDigitalRadio", "analogueFrequency", "archiveAudio"} <= pd.keys()

    mp3urls = [p["archiveAudio"]["mp3"] for p in resp_body] # TODO: this should be refactor

    for mp3url in mp3urls:
        # use head to save time, we don't really need to download the whole binary 
        h = requests.head(mp3url, allow_redirects=True)
        assert h.status_code == 200 # verify the server is responding ok status
        header = h.headers
        content_type = header.get('content-type')
        assert content_type == 'audio/mpeg' # verify that the content type is mp3
