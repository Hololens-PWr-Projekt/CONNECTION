from utils.constants import SERVER_URL, HOLOLENS_ID
import requests


def send_request_to_server(endpoint, method="GET", data=None):
    url = f"{SERVER_URL}{endpoint}"

    if method == "GET":
        response = requests.get(url)
    elif method == "POST":
        response = requests.post(url, json=data)
    else:
        raise ValueError("Unsupported HTTP method")

    return response.json()


def main():
    response = send_request_to_server("/")
    print("Server status:", response)

    # Get HoloLens data
    response = send_request_to_server(f"/hololens/data/{HOLOLENS_ID}")
    print(f"HoloLens data for {HOLOLENS_ID}:", response)


if __name__ == "__main__":
    main()
