
const urlParams = new URLSearchParams(window.location.search);
const playerId = urlParams.get('playerId');
const wsAddress = '{{wsAddress}}/game?playerId=' + playerId;

document.getElementById('playerId').innerText = 'Player ' + (parseInt(playerId) + 1);
const socket = new WebSocket(wsAddress);

let currentCardInPlay = null;
let currentHand = null;
let forceToDraw = false;

socket.onopen = function () {
    document.getElementById('status').innerText = 'Connected';
    socket.send(JSON.stringify({ action: 'GetGameStatus', id: playerId }));
};

socket.onmessage = function (event) {
    const data = JSON.parse(event.data);
    if (data.name == "GetGameStatus") {
        console.log(data.message);
        if (data.message == "START") {
            socket.send(JSON.stringify({ action: 'GetHand', id: playerId }));
            socket.send(JSON.stringify({ action: 'GetCurrentCard', id: playerId }));
        }
    }
    else if (data.name == "SetDraw"){
        forceToDraw = true;
    }
    else if (data.name == "GetCurrentCard") {
        currentCardInPlay = JSON.parse(data.message);
        updateHand(currentHand);
        updateCurrentCard(currentCardInPlay);
        console.log("Current card in play:", currentCardInPlay);
    }
    else if (data.name == "GetHand"){
        currentHand = JSON.parse(data.message);
        updateHand(JSON.parse(data.message));
    }
};

document.querySelector(".resetClick").addEventListener('click', () => {
    document.querySelectorAll('.card').forEach(c => c.classList.remove('selected'));
    selectedCard = null;
});

function isPlayable(card, currentCard) {
    if(forceToDraw && currentCard.number == 14){
        return card.number == currentCard.number;
    }
    if(forceToDraw && currentCard.number == 12){
        return card.number == currentCard.number || card.number == 14;
    }
    return card.number === 13 || card.number === 14 || // Wild cards
        card.color === currentCard.color ||            // Matching color
        card.number === currentCard.number;            // Matching number
}

var selectedCard = null;
const modal = document.getElementById("modalSelectColor")

function changeSelectedCardColor(color) {
    if (selectedCard) {
        selectedCard.color = color;
        socket.send(JSON.stringify({ action: 'PlayCard', id: playerId, card: selectedCard }));
        selectedCard = null;
    }
    modal.style.display = "none";
}
function updateHand(hand) {
    const cardContainer = document.getElementById('cards');
    cardContainer.innerHTML = '';

    const totalCards = hand.length;
    const baseOverlap = 30;  // Positive so cards are laid out left-to-right
    const maxOverlap = 40;
    const maxCurve = 30;     // Positive for upward curve
    const curveHeight = 4;

    // Calculate dynamic overlap and curve based on number of cards
    const overlapAmount = baseOverlap + (maxOverlap - baseOverlap) * (totalCards / 10);
    const curveAmount = maxCurve / totalCards;
    const middleIndex = (totalCards - 1) / 2;

    const colorOrder = ["Black", "Red", "Yellow", "Blue", "Green"];

    hand.sort((a, b) => {
        // Sort by color first
        const colorComparison = colorOrder.indexOf(a.color) - colorOrder.indexOf(b.color);
        if (colorComparison !== 0) {
            return colorComparison; // If colors are different, sort based on color
        }

        // If colors are the same, sort by number
        return b.number - a.number;
    });
    // Iterate over cards in reverse to ensure the newest card is on the right
    hand.forEach((card, index) => {
        const cardDiv = document.createElement('div');
        cardDiv.classList.add('card');
        var color;
        switch (card.color) {
            case "Yellow":
                color = "#ffc600";
                break;
            case "Blue":
                color = "#0006ff";
                break;
            case "Red":
                color = "#ed0000";
                break;
            case "Green":
                color = "#00c80e";
                break;
            default:
                color = "#080808";
                break;
        }
        cardDiv.style.backgroundColor = color;

        if (card.number < 10) {
            const cardNumberUpper = document.createElement('div');
            cardNumberUpper.classList.add('card-number-upper');
            cardNumberUpper.innerText = card.number;
            cardDiv.appendChild(cardNumberUpper);

            const cardNumberMiddle = document.createElement('div');
            cardNumberMiddle.classList.add('card-number-middle');
            cardNumberMiddle.innerText = card.number;
            cardNumberMiddle.style.color = color;
            cardDiv.appendChild(cardNumberMiddle);

            const cardNumberLower = document.createElement('div');
            cardNumberLower.classList.add('card-number-lower');
            cardNumberLower.innerText = card.number;
            cardDiv.appendChild(cardNumberLower);
        } else if (card.number == 10) // skip
        {
            const cardNumberUpper = document.createElement('div');
            cardNumberUpper.classList.add('card-number-upper');
            cardNumberUpper.innerText = "Ø";
            cardDiv.appendChild(cardNumberUpper);

            const cardNumberMiddle = document.createElement('div');
            cardNumberMiddle.classList.add('card-number-middle');
            cardNumberMiddle.innerText = "Ø";
            cardNumberMiddle.style.color = color;
            cardDiv.appendChild(cardNumberMiddle);

            const cardNumberLower = document.createElement('div');
            cardNumberLower.classList.add('card-number-lower');
            cardNumberLower.innerText = "Ø";
            cardDiv.appendChild(cardNumberLower);
        } else if (card.number == 11) // reverse
        {
            const cardNumberUpper = document.createElement('div');
            cardNumberUpper.classList.add('card-number-upper');
            cardNumberUpper.innerText = "↺";
            cardDiv.appendChild(cardNumberUpper);

            const cardNumberMiddle = document.createElement('div');
            cardNumberMiddle.classList.add('card-number-middle');
            cardNumberMiddle.innerText = "↺";
            cardNumberMiddle.style.color = color;
            cardDiv.appendChild(cardNumberMiddle);

            const cardNumberLower = document.createElement('div');
            cardNumberLower.classList.add('card-number-lower');
            cardNumberLower.innerText = "↺";
            cardDiv.appendChild(cardNumberLower);
        } else if (card.number == 12) // draw 2
        {
            const cardNumberUpper = document.createElement('div');
            cardNumberUpper.classList.add('card-number-upper');
            cardNumberUpper.innerText = "+2";
            cardNumberUpper.style.left = "0";
            cardNumberUpper.style.top = "-6px";
            cardNumberUpper.style.letterSpacing = "0";
            cardDiv.appendChild(cardNumberUpper);

            const cardNumberLower = document.createElement('div');
            cardNumberLower.classList.add('card-number-lower');
            cardNumberLower.innerText = "+2";
            cardNumberLower.style.letterSpacing = "0";
            cardNumberLower.style.right = "6px";
            cardDiv.appendChild(cardNumberLower);
        } else if (card.number == 13) // wild
        {
            const cardNumberUpper = document.createElement('div');
            cardNumberUpper.classList.add('card-number-upper');
            cardNumberUpper.classList.add('wild-symbol');
            cardDiv.appendChild(cardNumberUpper);

            const cardNumberMiddle = document.createElement('div');
            cardNumberMiddle.classList.add('card-number-middle');
            cardNumberMiddle.classList.add('wild-symbol');
            cardDiv.appendChild(cardNumberMiddle);

            const cardNumberLower = document.createElement('div');
            cardNumberLower.classList.add('card-number-lower');
            cardNumberLower.classList.add('wild-symbol');
            cardDiv.appendChild(cardNumberLower);
        } else if (card.number == 14) // draw 4
        {
            const cardNumberUpper = document.createElement('div');
            cardNumberUpper.classList.add('card-number-upper');
            cardNumberUpper.innerText = "+4";
            cardNumberUpper.style.letterSpacing = "0";
            cardNumberUpper.style.left = "0";
            cardNumberUpper.style.top = "-6px";
            cardDiv.appendChild(cardNumberUpper);

            const cardNumberMiddle = document.createElement('div');
            cardNumberMiddle.classList.add('card-number-middle');
            cardNumberMiddle.classList.add('wild-symbol');
            cardDiv.appendChild(cardNumberMiddle);

            const cardNumberLower = document.createElement('div');
            cardNumberLower.classList.add('card-number-lower');
            cardNumberLower.innerText = "+4";
            cardNumberLower.style.letterSpacing = "0";
            cardNumberLower.style.right = "6px";
            cardDiv.appendChild(cardNumberLower);
        }

        // Calculate position and rotation for each card
        const xPos = (index - middleIndex) * overlapAmount;   // Spread cards evenly along the x-axis
        const curveOffset = Math.abs(index - middleIndex) * curveHeight;  // Bend the outer cards upward
        const rotationAngle = (index - middleIndex) * curveAmount;  // Apply a curve to the card's rotation

        cardDiv.style.transform = `translate(${xPos}px, ${curveOffset}px) rotate(${rotationAngle}deg)`;

        if (currentCardInPlay && !isPlayable(card, currentCardInPlay)) {
            cardDiv.classList.add('unplayable');
            cardDiv.style.opacity = '0.5';  // Grey out the card
        } else {
            cardDiv.style.opacity = '1';   // Make playable cards fully visible

            // Allow clicking only on playable cards
            cardDiv.addEventListener('click', function () {
                if (cardDiv.classList.contains('selected')) {
                    if (card.number == 13 || card.number == 14) {
                        selectedCard = card;
                        modal.style.display = "block";
                    }
                    else {
                        cardDiv.classList.remove('selected');
                        selectedCard = null;
                        forceToDraw = false;
                        socket.send(JSON.stringify({ action: 'PlayCard', id: playerId, card: card }));
                    }
                } else {
                    document.querySelectorAll('.card').forEach(c => c.classList.remove('selected'));
                    cardDiv.classList.add('selected');
                    selectedCard = card;
                }
            });
        }

        cardContainer.appendChild(cardDiv);
    });
}

function updateCurrentCard(card) {
    const cardDiv = document.createElement('div');
    cardDiv.classList.add('card');
    var color;
    switch (card.color) {
        case "Yellow":
            color = "#ffc600";
            break;
        case "Blue":
            color = "#0006ff";
            break;
        case "Red":
            color = "#ed0000";
            break;
        case "Green":
            color = "#00c80e";
            break;
        default:
            color = "#080808";
            break;
    }
    cardDiv.style.backgroundColor = color;

    if (card.number < 10) {
        const cardNumberUpper = document.createElement('div');
        cardNumberUpper.classList.add('card-number-upper');
        cardNumberUpper.innerText = card.number;
        cardDiv.appendChild(cardNumberUpper);

        const cardNumberMiddle = document.createElement('div');
        cardNumberMiddle.classList.add('card-number-middle');
        cardNumberMiddle.innerText = card.number;
        cardNumberMiddle.style.color = color;
        cardDiv.appendChild(cardNumberMiddle);

        const cardNumberLower = document.createElement('div');
        cardNumberLower.classList.add('card-number-lower');
        cardNumberLower.innerText = card.number;
        cardDiv.appendChild(cardNumberLower);
    } else if (card.number == 10) // skip
    {
        const cardNumberUpper = document.createElement('div');
        cardNumberUpper.classList.add('card-number-upper');
        cardNumberUpper.innerText = "Ø";
        cardDiv.appendChild(cardNumberUpper);

        const cardNumberMiddle = document.createElement('div');
        cardNumberMiddle.classList.add('card-number-middle');
        cardNumberMiddle.innerText = "Ø";
        cardNumberMiddle.style.color = color;
        cardDiv.appendChild(cardNumberMiddle);

        const cardNumberLower = document.createElement('div');
        cardNumberLower.classList.add('card-number-lower');
        cardNumberLower.innerText = "Ø";
        cardDiv.appendChild(cardNumberLower);
    } else if (card.number == 11) // reverse
    {
        const cardNumberUpper = document.createElement('div');
        cardNumberUpper.classList.add('card-number-upper');
        cardNumberUpper.innerText = "↺";
        cardDiv.appendChild(cardNumberUpper);

        const cardNumberMiddle = document.createElement('div');
        cardNumberMiddle.classList.add('card-number-middle');
        cardNumberMiddle.innerText = "↺";
        cardNumberMiddle.style.color = color;
        cardDiv.appendChild(cardNumberMiddle);

        const cardNumberLower = document.createElement('div');
        cardNumberLower.classList.add('card-number-lower');
        cardNumberLower.innerText = "↺";
        cardDiv.appendChild(cardNumberLower);
    } else if (card.number == 12) // draw 2
    {
        const cardNumberUpper = document.createElement('div');
        cardNumberUpper.classList.add('card-number-upper');
        cardNumberUpper.innerText = "+2";
        cardNumberUpper.style.left = "0";
        cardNumberUpper.style.top = "-6px";
        cardNumberUpper.style.letterSpacing = "0";
        cardDiv.appendChild(cardNumberUpper);

        const cardNumberLower = document.createElement('div');
        cardNumberLower.classList.add('card-number-lower');
        cardNumberLower.innerText = "+2";
        cardNumberLower.style.letterSpacing = "0";
        cardNumberLower.style.right = "6px";
        cardDiv.appendChild(cardNumberLower);
    } else if (card.number == 13) // wild
    {
        const cardNumberUpper = document.createElement('div');
        cardNumberUpper.classList.add('card-number-upper');
        cardNumberUpper.classList.add('wild-symbol');
        cardDiv.appendChild(cardNumberUpper);

        const cardNumberMiddle = document.createElement('div');
        cardNumberMiddle.classList.add('card-number-middle');
        cardNumberMiddle.classList.add('wild-symbol');
        cardDiv.appendChild(cardNumberMiddle);

        const cardNumberLower = document.createElement('div');
        cardNumberLower.classList.add('card-number-lower');
        cardNumberLower.classList.add('wild-symbol');
        cardDiv.appendChild(cardNumberLower);
    } else if (card.number == 14) // draw 4
    {
        const cardNumberUpper = document.createElement('div');
        cardNumberUpper.classList.add('card-number-upper');
        cardNumberUpper.innerText = "+4";
        cardNumberUpper.style.letterSpacing = "0";
        cardNumberUpper.style.left = "0";
        cardNumberUpper.style.top = "-6px";
        cardDiv.appendChild(cardNumberUpper);

        const cardNumberMiddle = document.createElement('div');
        cardNumberMiddle.classList.add('card-number-middle');
        cardNumberMiddle.classList.add('wild-symbol');
        cardDiv.appendChild(cardNumberMiddle);

        const cardNumberLower = document.createElement('div');
        cardNumberLower.classList.add('card-number-lower');
        cardNumberLower.innerText = "+4";
        cardNumberLower.style.letterSpacing = "0";
        cardNumberLower.style.right = "6px";
        cardDiv.appendChild(cardNumberLower);
    }
    const cardContainer = document.getElementById('currentCard');
    cardContainer.innerHTML = "";
    cardContainer.style.pointerEvents = "none";
    cardContainer.appendChild(cardDiv);
}