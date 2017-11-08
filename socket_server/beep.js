var io = require('socket.io')({
	transports: ['websocket'],
});

var html;
var fs = require('fs');
fs.readFile("public/index.html", function(err, resp){
	if(err){
		console.log(err);
	} else {
		html = resp;
	}
});

// SOCKET CONNECTION

io.attach(4567);


io.on('connection', function(socket){
	console.log('unity connected')

	socket.on('fly', function(){
		io.emit("move")
	})

	socket.on('disconnect', function(){
		console.log("Unity disconnected")
	})
})

var http = require('http');

var Router = require('node-simple-router');
var router = Router();

var server = http.createServer(router);

router.get("/left", function(request, response){
	io.emit("left");
	console.log("Left Left")
	response.writeHead(200, {'Content-Type': 'text/html'});
	response.write(html);
})

router.get("/right", function(request, response){
	io.emit("right");
	console.log("Right right")
	response.writeHead(200, {'Content-Type': 'text/html'});
	response.write(html);
})

router.get("/up", function(request, response){
	io.emit("up");
	console.log("UP UP and away")
	response.writeHead(200, {'Content-Type': 'text/html'});
	response.write(html);
})

router.get("/down", function(request, response){
	io.emit("down");
	console.log("DOWN DOWN")
	response.writeHead(200, {'Content-Type': 'text/html'});
	response.write(html);
})

router.get("/even-up", function(request, response){
	io.emit("evenup");
	console.log("EVEN UP")
	response.writeHead(200, {'Content-Type': 'text/html'});
	response.write(html);
})

router.get("/even-down", function(request, response){
	io.emit("evendown");
	console.log("EVEN DOWN")
	response.writeHead(200, {'Content-Type': 'text/html'});
	response.write(html);
})

router.get("/even-left", function(request, response){
	io.emit("evenleft");
	console.log("EVEN LEFT")
	response.writeHead(200, {'Content-Type': 'text/html'});
	response.write(html);
})

router.get("/even-right", function(request, response){
	io.emit("evenright");
	console.log("EVEN RIGHT");
	response.writeHead(200, {'Content-Type': 'text/html'});
	response.write(html);
})

router.get("/roll", function(request, response){
	io.emit("roll");
	console.log("Barrell Roll");
	response.writeHead(200, {'Content-Type': 'text/html'});
	response.write(html);
})

router.get("/reset", function(request, response){
	io.emit("reset");
	console.log("Reset");
	response.writeHead(200, {'Content-Type': 'text/html'});
	response.write(html);
})


server.listen(8080, function(){
	console.log("Server listening")
})


