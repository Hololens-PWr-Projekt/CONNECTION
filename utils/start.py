import subprocess
import sys
import time


def run_command(command):
    process = subprocess.Popen(command, shell=True)
    return process


def main():
    # Todo: server address is hardcoded, it must depend on SERVER_URL constant
    server_process = run_command(
        "cd server && uvicorn main:app --host 127.0.0.1 --port 8000 --reload"
    )
    time.sleep(2)
    client_process = run_command("python3 -m client.client")
    hololens_process = run_command("python3 -m hololens.hololens_client")

    try:
        server_process.wait()
    except KeyboardInterrupt:
        print("Stopping all processes...")
        server_process.terminate()
        client_process.terminate()
        hololens_process.terminate()
        sys.exit(0)


if __name__ == "__main__":
    main()
