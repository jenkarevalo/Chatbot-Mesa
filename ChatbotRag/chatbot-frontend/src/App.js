import React, { useState } from "react";
import axios from "axios";

function App() {
  const [question, setQuestion] = useState("");
  const [answer, setAnswer] = useState("");

  const askQuestion = async () => {
    const res = await axios.get(`http://localhost:5000/ask?question=${question}`);
    setAnswer(res.data.answer);
  };

  return (
    <div className="min-h-screen bg-gray-100 flex flex-col items-center p-6">
      <h1 className="text-3xl font-bold mb-4">Chatbot Empresarial</h1>

      <div className="bg-white shadow-lg rounded-xl p-6 w-full max-w-md">
        <h2 className="text-xl font-semibold mb-2">Hacer una pregunta</h2>
        <input
          type="text"
          value={question}
          onChange={(e) => setQuestion(e.target.value)}
          placeholder="Escribe tu pregunta..."
          className="w-full border rounded-lg px-3 py-2"
        />
        <button
          onClick={askQuestion}
          className="mt-2 px-4 py-2 bg-green-600 text-white rounded-lg"
        >
          Preguntar
        </button>

        {answer && (
          <div className="mt-4 p-3 border rounded-lg bg-gray-50">
            <strong>Respuesta:</strong> {answer}
          </div>
        )}
      </div>
    </div>
  );
}

export default App;
