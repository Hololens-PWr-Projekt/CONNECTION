import requests

from utils.constants import SERVER_URL, HOLOLENS_ID


def send_data_to_server(device_id, data):
    url = f"{SERVER_URL}/hololens/data"
    payload = {"device_id": device_id, "sensor_data": data}
    response = requests.post(url, json=payload)

    # Weird thing, if we remove 'prints' we get json parse error
    print("Status Code:", response.status_code)
    print("Response Text:", response.text)

    return response.json()


def main():
    sample_data = {
        "x": 1.23,
        "y": 4.56,
        "z": 7.89,
        "position": 4.34,
    }

    response = send_data_to_server(HOLOLENS_ID, sample_data)
    print("Server response:", response)


if __name__ == "__main__":
    main()
