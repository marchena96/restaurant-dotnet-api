using RestauranteAPI.DTOs;

namespace RestauranteAPI.Servicios.Interfaces
{
    public interface IListaEsperaService
    {
        public List<ListaEsperaResponseDTO> GettAllListaEsperas();

        public ListaEsperaResponseDTO GetListaEsperaById(int listaEsperaId);

        public ListaEsperaResponseDTO CrearListaEspera(ListaEsperaCreateDTO listaEsperaDTO);
        public ListaEsperaResponseDTO ActualizarListaEspera(int listaEsperaId, ListaEsperaCreateDTO listaEsperaDTO);

        public void PromoverAReserva(int listaEsperaId, int mesaId);
    }
}