using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SnakeController : MonoBehaviour {

    public GameObject Bloque; // prefab del bloque
    public GameObject Escenario; // prefab del escenario
    public int Alto, Ancho; //Dimensiones de la pantalla
    public GameObject Item; //prefab del item
    public Text Puntuacion; // Texto del canvas de la puntuacion
    public float FuerzaExplosion = 200f;

    private Queue<GameObject> cuerpo = new Queue<GameObject>(); //cola de objetos que conforman la serpiente
    private GameObject cabeza;
    private GameObject item; //item instanciado en la escena

    private Vector3 direccion = Vector3.right; //direccion que vaya por defecto hacia la derecha

    private enum TipoCasilla
    {
        Vacio, Obstaculo, Item
    }

    private TipoCasilla[,] mapa;

    private int puntos = 0;

    private void Awake()
    {
        mapa = new TipoCasilla[Ancho, Alto];
        float posicionInicialX = Ancho / 2;
        float posicionInicialY = Alto / 2;
        CrearMuros();
        InicializaSerpiente(posicionInicialX, posicionInicialY);
        //siempre tendremos referencia a la cabeza:
        cabeza = NuevoBloque(posicionInicialX, posicionInicialY);
        InstanciarItemEnPosicionAleatoria();
        StartCoroutine(Movimiento());
    }

    private void IncrementarPuntos()
    {
        puntos++;
        Puntuacion.text = puntos.ToString();
    }

    private void Update()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 direccionSeleccionada = new Vector3(horizontal, vertical);
        if (direccionSeleccionada != Vector3.zero) //Si hemos clickado en horizontal o vertical
        {
            direccion = direccionSeleccionada; //guardaremos la posicion siguiente para cuando nos movamos (Movimiento())
        }
    }

    private void MoverItemAPosicionAleatoria()
    {
        Vector3 posicion = ObtenerPosicionVaciaAleatoria();
        item.transform.position = posicion;
        EstablecerMapa(item.transform.position, TipoCasilla.Item);
    }

    private void InstanciarItemEnPosicionAleatoria()
    {
        Vector3 posicion = ObtenerPosicionVaciaAleatoria();
        item = NuevoItem(Mathf.RoundToInt(posicion.x), Mathf.RoundToInt(posicion.y));
    }

    private Vector3 ObtenerPosicionVaciaAleatoria()
    {
        List<Vector3> posicionesVacias = new List<Vector3>();
        for (int x = 0; x < Ancho; x++) //Doble bucle para rellenar con bloques
        {
            for (int y = 0; y < Alto; y++)
            {
                if(mapa[x, y] == TipoCasilla.Vacio)
                {
                    posicionesVacias.Add(new Vector3(x, y));
                }
            }
        }
        return posicionesVacias[Random.Range(0, posicionesVacias.Count)];
    }

    private TipoCasilla ObtenerMapa(Vector3 posicion)
    {
        return mapa[Mathf.RoundToInt(posicion.x), Mathf.RoundToInt(posicion.y)];
    }

    private void EstablecerMapa(Vector3 posicion, TipoCasilla valor)
    {
        mapa[Mathf.RoundToInt(posicion.x), Mathf.RoundToInt(posicion.y)] = valor;
    }

    private GameObject NuevoBloque(float x, float y)
    {
        //Intancia un bloque principal en medio de la pantalla y lo encola en nuestra cola
        GameObject nuevo = Instantiate(Bloque, new Vector3(x, y), Quaternion.identity, this.transform);
        cuerpo.Enqueue(nuevo);
        //Lo ponemos como ocupado ese bloque que hayamos construido
        EstablecerMapa(nuevo.transform.position, TipoCasilla.Obstaculo);
        return nuevo;
    }

    private GameObject NuevoItem(int x, int y)
    {
        GameObject nuevo = Instantiate(Item, new Vector3(x, y), Quaternion.identity, Escenario.transform);
        EstablecerMapa(nuevo.transform.position, TipoCasilla.Item);
        return nuevo;
    }

    private void InicializaSerpiente(float x, float y)
    {
        for (int c = 15; c > 0; c--)
        {
            NuevoBloque(x - c, y);
        }
    }

    /// <summary>
    /// Lo que hace este metodo es que cuando movemos la serpiente hacia alguna dirección el último elemento de la cola 
    /// lo trasladamos a la cabeza haciendo el efecto de que se va moviendo de posicion en posicion
    /// </summary>
    /// <returns></returns>
    private IEnumerator Movimiento()
    {
        WaitForSeconds espera = new WaitForSeconds(0.15f);

        while (true)
        {
            Vector3 nuevaPosicion = cabeza.transform.position + direccion; // la nueva posicion del bloque siguiente será
            TipoCasilla casillaAOcupar = ObtenerMapa(nuevaPosicion);

            if(casillaAOcupar == TipoCasilla.Obstaculo) //si la casilla siguiente es un obstaculo
            {
                Muerte();
                yield return new WaitForSeconds(5); //espera 5 segundos
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); //recarga la escena
                yield break; //sal de la corrutina
            }
            else
            {
                GameObject parteCuerpo = new GameObject();
                if(casillaAOcupar == TipoCasilla.Item)
                {
                    parteCuerpo = NuevoBloque(nuevaPosicion.x, nuevaPosicion.y);
                    MoverItemAPosicionAleatoria();
                    IncrementarPuntos();
                }
                else
                {
                    parteCuerpo = cuerpo.Dequeue(); //sacara la primera de la cola
                    EstablecerMapa(parteCuerpo.transform.position, TipoCasilla.Vacio); //antes de moverlo está vacío
                    parteCuerpo.transform.position = nuevaPosicion; // a su posicion le pondrá la siguiente posicion que le corresponde
                    EstablecerMapa(parteCuerpo.transform.position, TipoCasilla.Obstaculo); //después de moverlo ya lo hemos rellenado
                    cuerpo.Enqueue(parteCuerpo); //lo encolamos el siguiente cacho
                }

                cabeza = parteCuerpo; //ahora la cabeza será esa pieza

                yield return espera;
            }
        }
    }

    private void CrearMuros()
    {
        for (int x = 0; x < Ancho; x++) //Doble bucle para rellenar con bloques
        {
            for (int y = 0; y < Alto; y++)
            {
                //Solo en el caso de que sea el borde deberá instanciarlo
                if (x == 0 || x == Ancho - 1 || y == 0 || y == Alto - 1)
                {
                    Vector3 posicion = new Vector3(x, y);
                    Instantiate(Bloque, posicion, Quaternion.identity, Escenario.transform);
                    EstablecerMapa(posicion, TipoCasilla.Obstaculo); //Cada bloque que vayamos construyendo que esté ocupado
                }
            }
        }
    }

    private void Muerte()
    {
        Explotar(this.GetComponentsInChildren<Rigidbody>());
        Explotar(Escenario.GetComponentsInChildren<Rigidbody>());
        StartCoroutine(CambiaColoresCamara());
    }

    private IEnumerator CambiaColoresCamara()
    {
        var vecesAEjecutarColores = 8;
        var colores = new Color[]
        {
            Color.black,
            Color.magenta,
            Color.red,
            Color.blue
        };

        for (int i = 0; i < vecesAEjecutarColores; i++)
        {
            var pos = Random.Range(0, colores.Length);
            Color color = colores[pos];
            Camera.main.backgroundColor = color;

            if(i == vecesAEjecutarColores - 1)
            {
                yield break;
            }
            else
            {
                yield return new WaitForSeconds(0.3f);
            }
        }
    }

    private void Explotar(Rigidbody[] rbs)
    {
        foreach (Rigidbody r in rbs)
        {
            r.useGravity = true;
            r.AddForce(Random.insideUnitCircle.normalized * FuerzaExplosion);
            r.AddTorque(0, 0, Random.Range(-FuerzaExplosion, FuerzaExplosion));
        }
    }
}
