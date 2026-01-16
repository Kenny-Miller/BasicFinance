export const enum ResultState {
  LOADING,
  LOADED,
  ERROR,
}

export type LoadingState = {
  state: ResultState.LOADING;
};
