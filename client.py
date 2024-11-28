from utils.constants import SERVER_URL
import requests


# Funkcja do wysy³ania ¿¹dañ HTTP do serwera
def send_request_to_server(endpoint, method="GET", data=None):
    url = f"{SERVER_URL}{endpoint}"  # Konstrukcja pe³nego URL na podstawie sta³ych i endpointu

    if method == "GET":
        response = requests.get(url)
    elif method == "POST":
        response = requests.post(url, json=data)
    else:
        raise ValueError("Unsupported HTTP method")

    return response.json()


def main():
    # Pobranie statusu serwera
    response = send_request_to_server("/")
    print("Server status:", response)

    # Pobranie dostêpnych typów wiadomoœci
    message_types = send_request_to_server("/message-types")
    print("Available message types:", message_types)

    # Pobranie danych dla urz¹dzenia HoloLens
    response = send_request_to_server(f"/hololens/data/HOLO1")
    print(f"HoloLens data:", response)


if __name__ == "__main__":
    main()
