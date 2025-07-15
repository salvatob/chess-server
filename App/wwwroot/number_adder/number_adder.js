
const input = document.getElementById('numberInput')
const display = document.getElementById('displayNumber')
const requestUrl = "/"


async function updateNumber() {
    const inputValue = parseInt(input.value, 10);

    if (!isNaN(inputValue)) {
        
        const newValue = await getFetch(inputValue)
        
        display.textContent = "" + newValue;
    } else {
        alert('Please enter a valid number.');
    }
}

const  getFetch = async (num) => {
    const numStringified = num.toString()
    console.log(`${typeof num} of value ${num}`)
    return fetch(requestUrl+("number_adder/" + numStringified))
    .then(res => res.json())
    .then((data) => {
        if (typeof data !== "number") {
            return NaN
        } else {
             return data
        }

    })
    .catch(()=> NaN)
}

async function RedirectToMessenger() {
    window.location.href = "../messenger/"
}
