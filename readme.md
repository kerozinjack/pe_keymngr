# Pannon Egyetem - Projekt Labor 2026/27/1 

A projekt a Projekt Labor tárgy keretein belül készült. Az alkalmazás egy kulcsnyílvántartó rendszert valósít meg, amely a teremhasználat és kulcskezelés teljes életciklusát digitálisan lefedi — a foglalástól a kulcs kiadásán és visszavételén át a riportálásig és auditálásig.  


## Build és futtatás:
1. Repository klónozása
- 1. HTTPS használatával
```sh
git clone https://github.com/kerozinjack/pe_keymngr.git
```
- 2. SSH használatával
```sh
git clone git@github.com:kerozinjack/pe_keymngr.git
```

2. Futtatás
```sh
cd pe_keymngr
docker compose up --build
```
Az alkalmazás a 8080 (HTTP) és 8081 (HTTPS) portokon üzemel
