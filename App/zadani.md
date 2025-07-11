<html lang="cz">

# Zadání Zápočtového programu
Projekt bude za zimní i letní semestr dohromady (NPRG035 i NPRG038).


### Funkcionalita
- Projekt bude rozdělen na dvě části
  1. Paralelní šachový bot
  2. Webová aplikace

Chessbot část je relativně přímočará. Půjde o konzolovou aplikaci, která přes textový protokol [UCI](https://en.wikipedia.org/wiki/Universal_Chess_Interface) vrátí nejlepší nalezený šachový tah. Už jsem takový projekt dělal minulý rok, nicméně kvalita je pro mě v tuto chvíli tak nedostatečná, že recyklace větších části kódu je prudce pravděpodobná. V případě, že bych vůbec něco zkopíroval, takovou část kódu označím, ale plánuji spíše vše přepsat znova.

Hlavní zaměření šachového enginu nicméně nebude čistě výpočetní výkon, ani všemožné "chytré" algoritmické prohledávání. 
Chtěl bych prohledávací algoritmus navrhnout tak, aby umožňoval efektivní využití více vláken, potažmo jader a prohledával stavový prostor paralelně.
Bude mít možnost nastavit obtížnost, aby hráče hra bavila.


Druhá část je webová aplikace. Zde pomocí __.NET Core MinimalAPI__ napíšu backend pro "univerzální" webovou stránku. Hlavní část bude možnost hrát šachy, buď s botem, nebo proti jiné osobě. Zde využiju nějaké dostupně webové chess GUI. Celý projekt rozhodně není zaměřen na frontend, ten bude dosti minimalistický a jednoduchý.

Sekundární účel stránky bude něco jako messenger. Aneb prostředí, kde se dají posílat zprávy mezi klienty. Možná bude i podpora pro posílání smajlíků, obrázků, podpora osobního profilu (username, profilová fotka).

### Motivace
Šachový engine je něco, co mě zajímá už dlouho. Vlastně ani nehraju šachy, ale baví mě algoritmy týkající se teorii her (minimax a a jeho rozšíření). 
Teď si připadám dostatečně schopný tento projekt navrhnout a implementovat v takové kvalitě, že s ním budu plně spokojený, a půjde ho v budoucnu vylepšovat.

Nějaká osobní webová stránka, obsahující moje projekty je podle mě nejlepší způsob, jak prezentovat svoje projekty a schopnosti široké veřejnosti. Například když budu chtít zjistit, zda je můj engine lepší než moji kamarádi, je pro ně mnohem dostupnější si zahrát online, než abych jim posílal exe soubory, a ještě si museli stahovat nějaké vlastní GUI.

### Dodatky
Pokud by to nevadilo, projekt bych ukázal přes localhost klienta. Nedokážu zajistit, že bych měl k dispozici veřejný server, na kterém bych web hostoval.

Vzhledem k tomu, že má projekt dvě části, mi dává smysl rozdělit i dokumentaci, tudíž budou dvě.