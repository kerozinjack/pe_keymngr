# University of Pannonia - Project Laboratory 2026/27/1 

This project was developed within the framework of the Project Laboratory course. The application implements a key management system that digitally covers the entire lifecycle of room usage and key management — from booking, issuing, and returning keys, through to reporting and auditing.  

## Build and Run:
1. Clone the repository
- Using HTTPS
```sh
git clone https://github.com/kerozinjack/pe_keymngr.git
```
- Using SSH
```sh
git clone git@github.com:kerozinjack/pe_keymngr.git
```

2. Run
```sh
cd pe_keymngr
docker compose up --build
```
The application runs on ports 8080 (HTTP) and 8081 (HTTPS).