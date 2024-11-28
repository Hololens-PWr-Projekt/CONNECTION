from utils.constants import SERVER_URL, HOLOLENS_ID
import requests


# Funkcja do przesyłania danych do serwera
def send_data_to_server(device_id, data):
    url = f"{SERVER_URL}/hololens/data"  # Endpoint do przesyłania danych
    payload = {"device_id": device_id, "sensor_data": data}  # Dane w formacie JSON
    response = requests.post(url, json=payload)

    # Debugowanie - wydruki statusu odpowiedzi
    print("Status Code:", response.status_code)
    print("Response Text:", response.text)
    return response.json()


# Funkcja do pobrania dostępnych typów wiadomości z serwera
def get_message_types():
    url = f"{SERVER_URL}/message-types"
    response = requests.get(url)
    return response.json()


# Funkcja walidująca dane przed wysłaniem zgodnie z typem wiadomości
def validate_data(message_type, data, message_types):
    expected_structure = message_types.get(message_type)  # Pobranie struktury wiadomości
    if not expected_structure:
        raise ValueError(f"Message type '{message_type}' is not registered")

    for key, expected_type in expected_structure.items():
        if key not in data:
            raise ValueError(f"Missing key: {key}")  # Sprawdzenie brakujących kluczy
        if not isinstance(data[key], eval(expected_type)):
            raise ValueError(f"Key '{key}' has invalid type. Expected {expected_type}, got {type(data[key]).__name__}")

    return True


def main():
    # Pobranie dostępnych typów wiadomości
    message_types = get_message_types()
    print("Available message types:", message_types)

    # Przykładowe dane do wysłania
    message_type = "positionData"  # Nazwa typu wiadomości
    sample_data = {
        "x": 1.23,
        "y": 4.56,
        "z": 7.89,
        "position": 4.34,
    }

    # Walidacja danych
    try:
        validate_data(message_type, sample_data, message_types)
    except ValueError as e:
        print("Validation error:", e)
        return

    # Wysłanie danych na serwer
    response = send_data_to_server(HOLOLENS_ID, sample_data)
    print("Server response:", response)


if __name__ == "__main__":
    main()
